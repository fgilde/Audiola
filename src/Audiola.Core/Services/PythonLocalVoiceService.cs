using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Audiola.Models;

namespace Audiola.Services;

/// <summary>
/// Lokale Voice-Engine über das Python-Sidecar-Skript <c>voicebox_engine.py</c>
/// (gleiche Mechanik wie der Demucs-Aufruf). Liefert bei fehlendem Skript/Python
/// eine eingebaute Modell-Liste, damit die Oberfläche nutzbar bleibt.
/// </summary>
public sealed class PythonLocalVoiceService : ILocalVoiceService
{
    private readonly ISettingsService _settings;
    private readonly IPythonEnvironment _env;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(15) };

    public PythonLocalVoiceService(ISettingsService settings, IPythonEnvironment env)
    {
        _settings = settings;
        _env = env;
    }

    public string ScriptPath =>
        Path.Combine(AppContext.BaseDirectory, "voicebox_engine.py");

    public bool ScriptAvailable => File.Exists(ScriptPath);

    /// <summary>Eingebaute Modell-Liste (Fallback + Basis für den Install-Status).</summary>
    private static readonly LocalVoiceModel[] Catalog =
    [
        new("kokoro", "Kokoro 82M", "Schnelles Preset-TTS (CPU-tauglich). pip install kokoro soundfile.", "tts", 330, false),
        new("qwen3-tts", "Qwen3-TTS 1.7B", "Mehrsprachiges Cloning. pip install qwen-tts.", "both", 1700, false),
        new("xtts-v2", "Coqui XTTS v2", "Mehrsprachig, Zero-Shot-Cloning aus einem Sample. pip install TTS.", "both", 1900, false),
        new("chatterbox", "Chatterbox", "Cloning aus einem Sample. pip install chatterbox-tts.", "both", 3000, false),
        new("seed-vc", "seed-vc (Stimmtausch)", "Zero-Shot Voice Conversion für Sprache + Gesang — erhält Melodie/Timing.", "clone", 1500, false),
    ];

    private static readonly LocalVoiceModel[] WhisperCatalog =
    [
        new("whisper-tiny", "Whisper tiny", "Sehr schnell, geringste Genauigkeit.", "transcribe", 75, false),
        new("whisper-base", "Whisper base", "Schnell, ordentliche Genauigkeit.", "transcribe", 145, false),
        new("whisper-small", "Whisper small", "Guter Kompromiss.", "transcribe", 480, false),
        new("whisper-medium", "Whisper medium", "Genauer, langsamer.", "transcribe", 1500, false),
        new("whisper-large-v3", "Whisper large-v3", "Höchste Genauigkeit.", "transcribe", 3000, false),
        new("whisper-turbo", "Whisper turbo", "Fast wie large, ~8× schneller.", "transcribe", 1600, false),
    ];

    public async Task<IReadOnlyList<LocalVoiceModel>> GetModelsAsync(CancellationToken ct = default)
    {
        if (!ScriptAvailable) return Catalog;
        try
        {
            var (code, stdout, _) = await RunAsync(["list-models", "--models-dir", ModelsDir], null, ct);
            if (code == 0 && !string.IsNullOrWhiteSpace(stdout))
            {
                var parsed = JsonSerializer.Deserialize<ModelsResponse>(ExtractJson(stdout), JsonOpts);
                if (parsed?.Models is { Count: > 0 })
                    return parsed.Models.Select(m => new LocalVoiceModel(
                        m.Id ?? "", m.Name ?? m.Id ?? "", m.Description ?? "", m.Capability ?? "tts",
                        m.SizeMb, m.Installed)).ToList();
            }
        }
        catch { /* Fallback unten */ }
        return Catalog;
    }

    public async Task<IReadOnlyList<LocalVoiceModel>> GetWhisperModelsAsync(CancellationToken ct = default)
    {
        if (!ScriptAvailable) return WhisperCatalog;
        try
        {
            var (code, stdout, _) = await RunAsync(["list-models", "--models-dir", ModelsDir], null, ct);
            if (code == 0 && !string.IsNullOrWhiteSpace(stdout))
            {
                var parsed = JsonSerializer.Deserialize<ModelsResponse>(ExtractJson(stdout), JsonOpts);
                if (parsed?.Whisper is { Count: > 0 })
                    return parsed.Whisper.Select(m => new LocalVoiceModel(
                        m.Id ?? "", m.Name ?? m.Id ?? "", m.Description ?? "", "transcribe", m.SizeMb, m.Installed)).ToList();
            }
        }
        catch { /* Fallback */ }
        return WhisperCatalog;
    }

    public async Task DownloadModelAsync(string modelId, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        RequireScript();

        // seed-vc ist repo-basiert: klonen + requirements installieren.
        if (string.Equals(modelId, "seed-vc", StringComparison.OrdinalIgnoreCase))
        {
            await ProvisionSeedVcAsync(progress, ct);
            return;
        }

        // 1) Isolierte Umgebung sicherstellen.
        await _env.EnsureAsync(progress, ct);

        // 2) torch (außer Whisper, das nutzt ctranslate2) – CUDA-Index bei NVIDIA-GPU.
        var isWhisper = modelId.StartsWith("whisper-", StringComparison.OrdinalIgnoreCase);
        if (!isWhisper)
            await _env.InstallAsync(["torch"], ShouldUseCuda() ? GpuDetect.CudaIndexUrl : null, progress, ct);

        // 3) Modell-Pakete.
        var pkgs = PackagesFor(modelId);
        if (pkgs.Count > 0) await _env.InstallAsync(pkgs, null, progress, ct);

        // 4) Gewichte laden / Installation verifizieren (über den Sidecar in der Umgebung).
        var (code, _, err) = await RunAsync(
            ["download", "--model", modelId, "--models-dir", ModelsDir, "--device", Device], progress, ct);
        if (code != 0)
            throw new InvalidOperationException($"Download fehlgeschlagen: {Short(err)}");
    }

    /// <summary>Stellt seed-vc bereit: Repo als ZIP laden + entpacken (kein Git nötig) + torch + requirements ins venv.</summary>
    private async Task ProvisionSeedVcAsync(IProgress<string>? progress, CancellationToken ct)
    {
        await _env.EnsureAsync(progress, ct);
        Directory.CreateDirectory(ModelsDir);
        var repo = Path.Combine(ModelsDir, "seed-vc");
        var inf = Path.Combine(repo, "inference.py");

        if (!File.Exists(inf))
        {
            progress?.Report("Lade seed-vc herunter …");
            var zipPath = Path.Combine(ModelsDir, $"_seedvc_{Guid.NewGuid():N}.zip");
            var extractDir = Path.Combine(ModelsDir, $"_extract_{Guid.NewGuid():N}");
            try
            {
                using (var resp = await Http.GetAsync(
                    "https://github.com/Plachtaa/seed-vc/archive/refs/heads/main.zip",
                    HttpCompletionOption.ResponseHeadersRead, ct))
                {
                    resp.EnsureSuccessStatusCode();
                    await using var fs = File.Create(zipPath);
                    await resp.Content.CopyToAsync(fs, ct);
                }

                progress?.Report("Entpacke seed-vc …");
                ZipFile.ExtractToDirectory(zipPath, extractDir);
                var inner = Directory.GetDirectories(extractDir).FirstOrDefault() ?? extractDir; // seed-vc-main
                if (Directory.Exists(repo)) Directory.Delete(repo, recursive: true);
                Directory.Move(inner, repo);
            }
            finally
            {
                try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
                try { if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true); } catch { }
            }

            if (!File.Exists(inf))
                throw new InvalidOperationException("seed-vc konnte nicht bereitgestellt werden (inference.py fehlt nach dem Entpacken).");
        }

        // WICHTIG: seed-vc hat Abhängigkeiten, die mit dem Haupt-venv KOLLIDIEREN
        // (seed-vc verlangt transformers==4.46.3, qwen-tts verlangt transformers==4.57.3).
        // Deshalb bekommt seed-vc ein EIGENES, isoliertes venv im Repo-Ordner; das Haupt-venv
        // (qwen-tts/Whisper) bleibt unberührt. Der Tausch ruft seed-vc mit diesem venv-Python auf.
        var venvPy = await EnsureSeedVcVenvAsync(repo, progress, ct);
        var depsMarker = Path.Combine(repo, ".venv", ".audiola_deps_ok");
        if (!File.Exists(depsMarker))
        {
            // 1) seed-vcs requirements.txt installieren. ACHTUNG: sie pinnt torch==2.4.0, das von PyPI
            //    als reiner CPU-Build kommt — deshalb wird torch danach ggf. durch CUDA ersetzt.
            await RunPipAsync(venvPy, ["-m", "pip", "install", "-r", Path.Combine(repo, "requirements.txt")], progress, ct);

            // 2) Bei NVIDIA-GPU das CPU-torch durch den passenden CUDA-Build (gleiche Version) ersetzen,
            //    sonst läuft die Diffusion auf der CPU und der Tausch dauert ewig.
            if (ShouldUseCuda())
            {
                progress?.Report("Installiere CUDA-Torch für seed-vc (GPU-Beschleunigung) …");
                await RunPipAsync(venvPy, ["-m", "pip", "install", "--force-reinstall", "--no-deps",
                    "torch==2.4.0", "torchvision==0.19.0", "torchaudio==2.4.0", "--index-url", GpuDetect.CudaIndexUrl], progress, ct);
            }
            await RunPipAsync(venvPy, ["-m", "pip", "install", "hf_xet"], progress, ct); // schnellerer HF-Download
            try { File.WriteAllText(depsMarker, "ok"); } catch { }
        }
    }

    /// <summary>
    /// Erstellt (einmalig) ein isoliertes venv im seed-vc-Repo und gibt dessen python.exe zurück.
    /// Getrennt vom Haupt-venv, damit seed-vcs transformers-Pin qwen-tts nicht zerstört.
    /// </summary>
    private async Task<string> EnsureSeedVcVenvAsync(string repo, IProgress<string>? progress, CancellationToken ct)
    {
        var venvDir = Path.Combine(repo, ".venv");
        var venvPy = Path.Combine(venvDir, "Scripts", "python.exe");
        if (!File.Exists(venvPy))
        {
            progress?.Report("Erstelle isolierte seed-vc-Umgebung (eigene Paketversionen) …");
            var (code, err) = await RunPipAsync(_env.PythonExe, ["-m", "venv", venvDir], progress, ct);
            if (code != 0 || !File.Exists(venvPy))
                throw new InvalidOperationException($"seed-vc-Umgebung konnte nicht erstellt werden: {Short(err)}");
            await RunPipAsync(venvPy, ["-m", "pip", "install", "--upgrade", "pip", "wheel"], progress, ct);
        }
        return venvPy;
    }

    /// <summary>Führt ein Programm (python/pip) aus und reicht die Ausgabe als Fortschritt durch.</summary>
    private static async Task<(int Code, string Err)> RunPipAsync(
        string exe, string[] args, IProgress<string>? progress, CancellationToken ct)
    {
        var r = await ProcessRunner.RunAsync(exe, args, progress, ct, ProcessRunner.StdoutMode.StreamLines);
        return (r.ExitCode, r.Stderr);
    }

    /// <summary>
    /// Stellt die Whisper-Transkription bereit (venv + faster-whisper), falls noch nicht geschehen.
    /// Idempotent über einen Marker, damit nicht bei jeder Transkription neu installiert wird.
    /// </summary>
    private async Task EnsureWhisperAsync(IProgress<string>? progress, CancellationToken ct)
    {
        await _env.EnsureAsync(progress, ct);
        Directory.CreateDirectory(ModelsDir);
        var marker = Path.Combine(ModelsDir, ".audiola_whisper_ok");
        if (File.Exists(marker)) return;

        progress?.Report("Installiere faster-whisper …");
        await _env.InstallAsync(["faster-whisper"], null, progress, ct);
        try { File.WriteAllText(marker, "ok"); } catch { /* Marker optional */ }
    }

    private static IReadOnlyList<string> PackagesFor(string modelId) => modelId switch
    {
        "qwen3-tts" => ["qwen-tts", "soundfile", "numpy"],
        "xtts-v2" => ["TTS", "soundfile", "numpy"],
        "kokoro" => ["kokoro", "soundfile", "numpy"],
        "chatterbox" => ["chatterbox-tts", "soundfile", "numpy"],
        _ when modelId.StartsWith("whisper-", StringComparison.OrdinalIgnoreCase) => ["faster-whisper"],
        _ => []
    };

    public async Task<GpuStatus> CheckGpuAsync(CancellationToken ct = default)
    {
        if (!_env.Exists) return new GpuStatus(false, false, "", "Python-Umgebung noch nicht eingerichtet (erst ein Modell „Laden“).");
        if (!ScriptAvailable) return new GpuStatus(false, false, "", "Sidecar-Skript fehlt.");
        try
        {
            var (code, stdout, err) = await RunAsync(["gpu-check"], null, ct);
            if (code != 0) return new GpuStatus(false, false, "", Short(err));
            var g = JsonSerializer.Deserialize<GpuDto>(ExtractJson(stdout), JsonOpts);
            if (g is null) return new GpuStatus(false, false, "", "Keine Antwort.");
            return new GpuStatus(g.Torch, g.Cuda, g.Device_Name ?? "", g.Error);
        }
        catch (Exception ex) { return new GpuStatus(false, false, "", ex.Message); }
    }

    public async Task InstallCudaTorchAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        await _env.EnsureAsync(progress, ct);
        progress?.Report("Installiere CUDA-Variante von torch/torchvision/torchaudio … (groß, dauert)");
        // Alle drei zusammen aus demselben Index → passende ABI (sonst „Einsprungpunkt nicht gefunden").
        await _env.InstallAsync(["torch", "torchvision", "torchaudio"], GpuDetect.CudaIndexUrl,
            progress, ct, forceReinstall: true);
    }

    public async Task<(float[] Samples, int SampleRate)> SpeakAsync(string text, VoiceProfile profile, double speed, CancellationToken ct = default)
    {
        RequireScript();
        var outWav = TempWav("tts");
        var args = new List<string>
        {
            "tts", "--model", profile.ModelId, "--text", text, "--language", profile.Language,
            "--device", Device, "--models-dir", ModelsDir, "--out", outWav,
            "--speed", speed.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
        };
        var speaker = profile.SamplePaths.FirstOrDefault(File.Exists);
        if (speaker is not null) { args.Add("--speaker"); args.Add(speaker); }

        var (code, _, err) = await RunAsync([.. args], null, ct);
        if (code != 0 || !File.Exists(outWav))
            throw new InvalidOperationException($"Lokale Sprachsynthese fehlgeschlagen: {Short(err)}");
        return AudioProcessingHelper.ReadStereo(outWav);
    }

    public async Task<(float[] Samples, int SampleRate)> ChangeVoiceAsync(string inputWav, VoiceProfile profile,
        int diffusionSteps = 50, bool autoF0Adjust = false,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        RequireScript();
        var speaker = profile.SamplePaths.FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException("Diese lokale Stimme hat kein Referenz-Sample für den Stimmtausch.");
        var outWav = TempWav("vc");

        // Sicherheits-Timeout, damit es nicht endlos hängt (CPU-Diffusion kann extrem lange dauern).
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMinutes(30));

        int code; string err;
        try
        {
            (code, _, err) = await RunAsync(
                ["vc", "--input", inputWav, "--speaker", speaker, "--device", Device, "--models-dir", ModelsDir, "--out", outWav,
                 "--diffusion-steps", diffusionSteps.ToString(System.Globalization.CultureInfo.InvariantCulture),
                 "--auto-f0-adjust", autoF0Adjust ? "True" : "False"],
                progress, cts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                "Lokaler Stimmtausch abgebrochen (Zeitüberschreitung nach 30 Min). seed-vc ist auf CPU sehr langsam — " +
                "bitte eine CUDA-GPU verwenden (Gerät in „Stimmen“ auf cuda) oder einen kürzeren Bereich markieren.");
        }

        if (code != 0 || !File.Exists(outWav))
        {
            var detail = err.Length > 1500 ? err[^1500..] : err;
            throw new InvalidOperationException($"Lokaler Stimmtausch fehlgeschlagen:\n{detail}");
        }
        return AudioProcessingHelper.ReadStereo(outWav);
    }

    public async Task<(float[] Samples, int SampleRate)> AutoTuneAsync(string inputWav, string referenceWav,
        double strength, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        RequireScript();
        await EnsureAutotuneAsync(progress, ct);
        var outWav = TempWav("tuned");
        var (code, _, err) = await RunAsync(
            ["autotune", "--input", inputWav, "--reference", referenceWav, "--out", outWav,
             "--strength", strength.ToString(System.Globalization.CultureInfo.InvariantCulture)],
            progress, ct);
        if (code != 0 || !File.Exists(outWav))
            throw new InvalidOperationException($"Tonhöhen-Korrektur fehlgeschlagen: {Short(err)}");
        return AudioProcessingHelper.ReadStereo(outWav);
    }

    /// <summary>Stellt die Tonhöhen-Korrektur bereit (WORLD-Vocoder). Idempotent über einen Marker.</summary>
    private async Task EnsureAutotuneAsync(IProgress<string>? progress, CancellationToken ct)
    {
        await _env.EnsureAsync(progress, ct);
        Directory.CreateDirectory(ModelsDir);
        var marker = Path.Combine(ModelsDir, ".audiola_autotune_ok");
        if (File.Exists(marker)) return;
        progress?.Report("Richte Tonhöhen-Korrektur ein (pyworld) …");
        await _env.InstallAsync(["pyworld", "soundfile"], null, progress, ct);
        try { File.WriteAllText(marker, "ok"); } catch { }
    }

    public async Task<IReadOnlyList<TranscriptSegment>> TranscribeAsync(string inputWav, string whisperModel, CancellationToken ct = default)
    {
        RequireScript();
        await EnsureWhisperAsync(null, ct);   // faster-whisper bei Bedarf selbst bereitstellen

        // Ergebnis in eine Datei schreiben lassen — robust gegen abgeschnittene stdout-Pipes bei langen Transkripten.
        var outPath = TempWav("lrc") + ".json";
        var (code, stdout, err) = await RunAsync(
            ["transcribe", "--input", inputWav, "--model", whisperModel, "--device", Device, "--out", outPath], null, ct);
        if (code != 0)
            throw new InvalidOperationException($"Transkription fehlgeschlagen: {Short(err)}");

        string json;
        try { json = File.Exists(outPath) ? await File.ReadAllTextAsync(outPath, ct) : ExtractJson(stdout); }
        finally { try { if (File.Exists(outPath)) File.Delete(outPath); } catch { /* egal */ } }

        var parsed = JsonSerializer.Deserialize<TranscriptResponse>(
            string.IsNullOrWhiteSpace(json) ? "{}" : json, JsonOpts);
        return parsed?.Segments?.Select(s => new TranscriptSegment(s.Start, s.End, (s.Text ?? "").Trim())).ToList()
               ?? [];
    }

    private string ModelsDir => _settings.Current.VoiceModelsDirectory;
    private string Device => string.IsNullOrWhiteSpace(_settings.Current.VoiceDevice) ? "auto" : _settings.Current.VoiceDevice;

    /// <summary>CUDA nutzen? Zentrale Entscheidung über <see cref="GpuDetect"/> (siehe dort).</summary>
    private bool ShouldUseCuda() => GpuDetect.ShouldUseCuda(Device);

    private void RequireScript()
    {
        if (!ScriptAvailable)
            throw new InvalidOperationException(
                "Lokale Voice-Engine nicht gefunden (voicebox_engine.py fehlt). Bitte das Skript bereitstellen " +
                "und die Python-Abhängigkeiten installieren.");
    }

    private static string TempWav(string tag)
    {
        var dir = Path.Combine(Path.GetTempPath(), "Audiola", "voice");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{tag}_{Guid.NewGuid():N}.wav");
    }

    private static string ExtractJson(string s)
    {
        var start = s.IndexOf('{');
        var end = s.LastIndexOf('}');
        return start >= 0 && end > start ? s[start..(end + 1)] : "{}";
    }

    private static string Short(string s) => string.IsNullOrEmpty(s) ? "(keine Ausgabe)" : s.Length > 300 ? s[^300..] : s;

    private async Task<(int Code, string Stdout, string Stderr)> RunAsync(
        string[] args, IProgress<string>? progress, CancellationToken ct)
    {
        // Bevorzugt die verwaltete Umgebung (mit allen installierten Paketen), sonst System-Python.
        // stdout im Buffer-Modus: lange JSON-Ausgaben (Transkript in einer Zeile) ohne Zeilen-Race.
        var exe = _env.Exists ? _env.PythonExe : _settings.Current.PythonPath;
        var r = await ProcessRunner.RunAsync(exe, [ScriptPath, .. args], progress, ct);
        return (r.ExitCode, r.Stdout, r.Stderr);
    }

    // ---- JSON-DTOs ----
    private sealed class ModelsResponse { public List<ModelDto>? Models { get; set; } public List<ModelDto>? Whisper { get; set; } }
    private sealed class ModelDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Capability { get; set; }
        public int SizeMb { get; set; }
        public bool Installed { get; set; }
    }
    private sealed class GpuDto
    {
        public bool Torch { get; set; }
        public bool Cuda { get; set; }
        public string? Device_Name { get; set; }
        public string? Torch_Version { get; set; }
        public string? Error { get; set; }
    }
    private sealed class TranscriptResponse { public List<SegmentDto>? Segments { get; set; } }
    private sealed class SegmentDto { public double Start { get; set; } public double End { get; set; } public string? Text { get; set; } }
}
