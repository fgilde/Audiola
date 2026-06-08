using System.Collections.Concurrent;
using Audiola.Models;
using Audiola.Services;

var builder = WebApplication.CreateBuilder(args);

// Große Audio-Uploads erlauben (Standard-Limit 30 MB → Verbindung wird sonst zurückgesetzt).
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = null);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = long.MaxValue;
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartHeadersLengthLimit = int.MaxValue;
});

// Geteilte Core-Logik als Dienste.
builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddSingleton<IMasteringService, MasteringService>();
builder.Services.AddSingleton<IStemSeparationService, DemucsStemSeparationService>();
builder.Services.AddSingleton<IWaveformService, WaveformService>();
builder.Services.AddSingleton<IProjectService, ProjectService>();
// Variations-Provider (nur die legitimen; eigene Provider hier ergänzen).
builder.Services.AddSingleton<IAudioVariationProvider, StudioEffectsVariationProvider>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

// WASM-Client (Audiola.Web) aus derselben App ausliefern.
app.UseBlazorFrameworkFiles();
app.UseRouting();

var tmpDir = Path.Combine(Path.GetTempPath(), "Audiola", "api");
var sessionsRoot = Path.Combine(tmpDir, "sessions");
Directory.CreateDirectory(sessionsRoot);

// sessionId -> Arbeitsverzeichnis (Stems + Mix)
var sessions = new ConcurrentDictionary<string, string>();

string SaveUpload(IFormFile file)
{
    Directory.CreateDirectory(tmpDir);
    var path = Path.Combine(tmpDir, $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}");
    using var fs = File.Create(path);
    file.CopyTo(fs);
    return path;
}

// --- Verfügbare Variations-Provider + ihre Variationen ---
app.MapGet("/api/variations", (IEnumerable<IAudioVariationProvider> providers) =>
    Results.Ok(providers.Select(p => new
    {
        provider = p.Name,
        variations = p.GetVariations().Select(v => new { v.Id, v.Name, v.Description })
    })));

// --- Mastering-Profile: eingebaute + eigene ---
app.MapGet("/api/mastering-profiles", (ISettingsService settings) =>
{
    var builtIn = MasteringProfiles.All.Select(p => new { name = p.Name, description = p.Description, builtin = true, settings = p.Settings });
    var user = settings.Current.UserMasteringProfiles.Select(u => new { name = u.Name, description = "Eigenes Profil", builtin = false, settings = u.Settings });
    return Results.Ok(builtIn.Concat(user));
});

app.MapPost("/api/mastering-profiles", (SaveProfileRequest req, ISettingsService settings) =>
{
    if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest("Name fehlt.");
    if (MasteringProfiles.All.Any(p => p.Name == req.Name)) return Results.Conflict("Name ist ein eingebautes Profil.");
    var list = settings.Current.UserMasteringProfiles;
    var existing = list.FirstOrDefault(u => u.Name == req.Name);
    if (existing is not null) existing.Settings = req.Settings;
    else list.Add(new SavedMasteringProfile { Name = req.Name, Settings = req.Settings });
    settings.Save();
    return Results.Ok();
}).DisableAntiforgery();

app.MapDelete("/api/mastering-profiles/{name}", (string name, ISettingsService settings) =>
{
    var list = settings.Current.UserMasteringProfiles;
    var u = list.FirstOrDefault(x => x.Name == name);
    if (u is not null) { list.Remove(u); settings.Save(); }
    return Results.Ok();
});

// --- Datei mastern (EQ → Kompressor → LUFS) → gemasterte WAV zurück ---
app.MapPost("/api/master", async (IFormFile file, IMasteringService mastering,
    double targetLufs = -14, bool normalize = true, bool compressor = true,
    double highPassHz = 30, double lowShelfGainDb = 0, double midGainDb = 0, double highShelfGainDb = 0,
    double thresholdDb = -18, double ratio = 2, double makeupGainDb = 0) =>
{
    var input = SaveUpload(file);
    var output = Path.Combine(tmpDir, $"master_{Guid.NewGuid():N}.wav");
    var s = new MasteringSettings
    {
        NormalizeLoudness = normalize,
        TargetLufs = targetLufs,
        CompressorEnabled = compressor,
        HighPassEnabled = highPassHz > 0,
        HighPassHz = highPassHz,
        LowShelfGainDb = lowShelfGainDb,
        MidGainDb = midGainDb,
        HighShelfGainDb = highShelfGainDb,
        ThresholdDb = thresholdDb,
        Ratio = ratio,
        MakeupGainDb = makeupGainDb
    };
    var result = await mastering.ProcessAndExportAsync(input, output, s);
    var bytes = await File.ReadAllBytesAsync(output);
    return Results.File(bytes, "audio/wav", "mastered.wav", enableRangeProcessing: true);
}).DisableAntiforgery();

// --- Variation(en) eines Providers anwenden → WAV zurück ---
app.MapPost("/api/apply-variation", async (IFormFile file, string provider, string ids,
    IEnumerable<IAudioVariationProvider> providers) =>
{
    var prov = providers.FirstOrDefault(p => p.Name == provider);
    if (prov is null) return Results.NotFound($"Provider '{provider}' nicht gefunden.");

    var input = SaveUpload(file);
    var (samples, sr) = AudioProcessingHelper.ReadStereo(input);
    foreach (var id in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        samples = await prov.ApplyAsync(id, samples, sr);

    var output = Path.Combine(tmpDir, $"var_{Guid.NewGuid():N}.wav");
    AudioEdits.WriteWav(output, samples, sr);
    return Results.File(await File.ReadAllBytesAsync(output), "audio/wav", "variation.wav", enableRangeProcessing: true);
}).DisableAntiforgery();

// --- Stems trennen (Demucs) → Session mit Stem-Dateien + Wellenform-Peaks ---
app.MapPost("/api/separate", async (IFormFile file, bool? auto, IStemSeparationService sep, IWaveformService waveform) =>
{
    var input = SaveUpload(file);
    var model = auto == true ? "htdemucs_6s" : null;
    var set = await sep.SeparateAsync(input, null, model, auto == true ? 1 : 0);

    var id = Guid.NewGuid().ToString("N");
    var dir = Path.Combine(sessionsRoot, id);
    Directory.CreateDirectory(dir);
    var stems = new List<object>();
    foreach (var st in set.Stems)
    {
        var name = Path.GetFileName(st.FilePath);
        var dest = Path.Combine(dir, name);
        File.Copy(st.FilePath, dest, overwrite: true);
        var wf = await waveform.LoadAsync(dest, 1600);
        stems.Add(new { kind = st.Kind.ToString(), file = name, durationSeconds = wf.Duration.TotalSeconds, peaks = wf.Peaks });
    }
    sessions[id] = dir;
    return Results.Ok(new { sessionId = id, stems });
}).DisableAntiforgery();

// --- Datei einer Session ausliefern (Stem oder gerenderter Mix) ---
app.MapGet("/api/session/{id}/file/{name}", (string id, string name) =>
{
    if (!sessions.TryGetValue(id, out var dir)) return Results.NotFound();
    var path = Path.Combine(dir, Path.GetFileName(name)); // GetFileName verhindert Pfad-Traversal
    if (!File.Exists(path)) return Results.NotFound();
    var ct = Path.GetExtension(path).ToLowerInvariant() == ".mp3" ? "audio/mpeg" : "audio/wav";
    return Results.File(path, ct, enableRangeProcessing: true);
});

// --- Mehrspur-Mix rendern (+ optional mastern) → WAV zurück ---
app.MapPost("/api/mix", async (MixRequest req, IMasteringService mastering) =>
{
    if (!sessions.TryGetValue(req.SessionId, out var dir)) return Results.NotFound();

    var mixTracks = req.Tracks
        .Select(t => new MixTrack(Path.Combine(dir, Path.GetFileName(t.File)), t.Volume, t.Pan, t.Muted, t.Solo, t.OffsetSeconds))
        .ToList();

    var (samples, sr) = OfflineMixer.Render(mixTracks);
    var mixPath = Path.Combine(dir, "mix.wav");
    AudioEdits.WriteWav(mixPath, samples, sr);

    if (req.Master)
    {
        var masteredPath = Path.Combine(dir, "mix_master.wav");
        await mastering.ProcessAndExportAsync(mixPath, masteredPath,
            new MasteringSettings { NormalizeLoudness = true, TargetLufs = req.TargetLufs });
        mixPath = masteredPath;
    }

    return Results.File(await File.ReadAllBytesAsync(mixPath), "audio/wav", "mix.wav", enableRangeProcessing: true);
}).DisableAntiforgery();

// --- Variation(en) auf einen Session-Stem anwenden (in-place) → neue Peaks/Dauer ---
app.MapPost("/api/session/{id}/apply-variation", async (string id, ApplyVarRequest req,
    IEnumerable<IAudioVariationProvider> providers, IWaveformService waveform) =>
{
    if (!sessions.TryGetValue(id, out var dir)) return Results.NotFound();
    var prov = providers.FirstOrDefault(p => p.Name == req.Provider);
    if (prov is null) return Results.NotFound($"Provider '{req.Provider}' nicht gefunden.");
    var path = Path.Combine(dir, Path.GetFileName(req.File));
    if (!File.Exists(path)) return Results.NotFound("Stem nicht gefunden.");

    var (samples, sr) = AudioProcessingHelper.ReadStereo(path);
    foreach (var vid in req.Ids)
        samples = await prov.ApplyAsync(vid, samples, sr);
    AudioEdits.WriteWav(path, samples, sr);

    var wf = await waveform.LoadAsync(path, 1600);
    return Results.Ok(new { durationSeconds = wf.Duration.TotalSeconds, peaks = wf.Peaks });
}).DisableAntiforgery();

// --- Schnitt/Effekt auf einen Session-Stem anwenden (in-place) → neue Peaks/Dauer ---
app.MapPost("/api/session/{id}/edit", async (string id, EditRequest req, IWaveformService waveform) =>
{
    if (!sessions.TryGetValue(id, out var dir)) return Results.NotFound();
    var path = Path.Combine(dir, Path.GetFileName(req.File));
    if (!File.Exists(path)) return Results.NotFound("Stem nicht gefunden.");

    var (samples, sr) = AudioProcessingHelper.ReadStereo(path);
    int a = (int)Math.Round(req.StartSeconds * sr);
    int b = (int)Math.Round(req.EndSeconds * sr);

    samples = req.Op.ToLowerInvariant() switch
    {
        "trim"     => AudioEdits.Trim(samples, a, b),
        "delete"   => AudioEdits.Delete(samples, a, b),
        "silence"  => AudioEdits.Silence(samples, a, b),
        "fadein"   => AudioEdits.Fade(samples, a, b, fadeIn: true),
        "fadeout"  => AudioEdits.Fade(samples, a, b, fadeIn: false),
        "normalize"=> AudioEffects.Normalize(samples, a, b),
        "reverse"  => AudioEffects.Reverse(samples, a, b),
        _ => samples
    };
    AudioEdits.WriteWav(path, samples, sr);

    var wf = await waveform.LoadAsync(path, 1600);
    return Results.Ok(new { durationSeconds = wf.Duration.TotalSeconds, peaks = wf.Peaks });
}).DisableAntiforgery();

// --- Mehrband-EQ auf eine hochgeladene Datei anwenden → WAV zurück ---
app.MapPost("/api/eq", async (HttpRequest http) =>
{
    var form = await http.ReadFormAsync();
    var file = form.Files["file"];
    if (file is null) return Results.BadRequest("Keine Datei.");
    var bandsJson = form["bands"].ToString();
    var bands = System.Text.Json.JsonSerializer.Deserialize<List<EqBandReq>>(bandsJson,
        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

    var input = SaveUpload(file);
    var (samples, sr) = AudioProcessingHelper.ReadStereo(input);

    Audiola.Dsp.Biquad Make(EqBandReq band) => band.Type.ToLowerInvariant() switch
    {
        "lowshelf"  => Audiola.Dsp.Biquad.LowShelf(sr, band.Frequency, 0.707, band.GainDb),
        "highshelf" => Audiola.Dsp.Biquad.HighShelf(sr, band.Frequency, 0.707, band.GainDb),
        _ => Audiola.Dsp.Biquad.Peaking(sr, band.Frequency, band.Q <= 0 ? 1.0 : band.Q, band.GainDb)
    };
    var chainL = bands.Select(Make).ToList();
    var chainR = bands.Select(Make).ToList();

    var frames = samples.Length / 2;
    for (var i = 0; i < frames; i++)
    {
        var l = samples[i * 2];
        var r = samples[i * 2 + 1];
        foreach (var f in chainL) l = f.Process(l);
        foreach (var f in chainR) r = f.Process(r);
        samples[i * 2] = Math.Clamp(l, -1f, 1f);
        samples[i * 2 + 1] = Math.Clamp(r, -1f, 1f);
    }

    var output = Path.Combine(tmpDir, $"eq_{Guid.NewGuid():N}.wav");
    AudioEdits.WriteWav(output, samples, sr);
    return Results.File(await File.ReadAllBytesAsync(output), "audio/wav", "equalized.wav", enableRangeProcessing: true);
}).DisableAntiforgery();

// --- Projekt speichern: Session-Stems + Mix-Einstellungen → .audiola zum Download ---
app.MapPost("/api/session/{id}/project", async (string id, SaveProjectRequest req, IProjectService projects) =>
{
    if (!sessions.TryGetValue(id, out var dir)) return Results.NotFound();

    var dto = new ProjectDto
    {
        MasterVolume = 1.0,
        PixelsPerSecond = req.PixelsPerSecond <= 0 ? 40 : req.PixelsPerSecond,
        Mastering = req.Master ? new MasteringSettings { NormalizeLoudness = true, TargetLufs = req.TargetLufs } : null,
        Tracks = req.Tracks.Select(t => new ProjectTrackDto
        {
            Name = Path.GetFileNameWithoutExtension(t.File),
            Volume = t.Volume,
            Pan = t.Pan,
            IsMuted = t.Muted,
            IsSolo = t.Solo,
            Clips =
            [
                new ProjectClipDto { Media = Path.Combine(dir, Path.GetFileName(t.File)), TimelineOffsetSeconds = t.OffsetSeconds }
            ]
        }).ToList()
    };

    var outPath = Path.Combine(tmpDir, $"project_{Guid.NewGuid():N}.audiola");
    await projects.SaveAsync(outPath, dto);
    return Results.File(await File.ReadAllBytesAsync(outPath), "application/octet-stream", "projekt.audiola");
}).DisableAntiforgery();

// --- Projekt laden: .audiola entpacken → neue Session + Spuren/Mix-Manifest ---
app.MapPost("/api/project/load", async (IFormFile file, IProjectService projects, IWaveformService waveform) =>
{
    var upload = SaveUpload(file);
    var dto = await projects.LoadAsync(upload);

    var id = Guid.NewGuid().ToString("N");
    var dir = Path.Combine(sessionsRoot, id);
    Directory.CreateDirectory(dir);

    var tracks = new List<object>();
    foreach (var t in dto.Tracks)
    {
        var clip = t.Clips.FirstOrDefault();
        if (clip is null || string.IsNullOrEmpty(clip.Media) || !File.Exists(clip.Media)) continue;
        var name = Path.GetFileName(clip.Media);
        var dest = Path.Combine(dir, name);
        File.Copy(clip.Media, dest, overwrite: true);
        var wf = await waveform.LoadAsync(dest, 1600);
        tracks.Add(new
        {
            kind = t.Name, file = name, volume = t.Volume, pan = t.Pan,
            muted = t.IsMuted, solo = t.IsSolo,
            offsetSeconds = clip.TimelineOffsetSeconds,
            durationSeconds = wf.Duration.TotalSeconds, peaks = wf.Peaks
        });
    }
    sessions[id] = dir;
    return Results.Ok(new
    {
        sessionId = id,
        tracks,
        master = dto.Mastering is not null,
        targetLufs = dto.Mastering?.TargetLufs ?? -14
    });
}).DisableAntiforgery();

// Fingerprinted Static Web Assets (inkl. _content/<RCL>/… JS-Initializer) ausliefern.
app.MapStaticAssets();
// Alle nicht-/api-Routen an den WASM-Client (index.html) — Client-seitiges Routing.
app.MapFallbackToFile("index.html");

app.Run();

// ---- Request-DTOs ----
record MixRequest(string SessionId, bool Master, double TargetLufs, List<MixTrackReq> Tracks);
record MixTrackReq(string File, double Volume, double Pan, bool Muted, bool Solo, double OffsetSeconds);
record ApplyVarRequest(string File, string Provider, List<string> Ids);
record SaveProfileRequest(string Name, Audiola.Models.MasteringSettings Settings);
record EditRequest(string File, string Op, double StartSeconds, double EndSeconds);
record EqBandReq(string Type, double Frequency, double GainDb, double Q);
record SaveProjectRequest(bool Master, double TargetLufs, double PixelsPerSecond, List<MixTrackReq> Tracks);
