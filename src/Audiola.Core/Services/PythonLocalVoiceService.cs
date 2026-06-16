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

        // 2) torch (außer Whisper, das nutzt ctranslate2) – CUDA-Index, falls gewünscht.
        var isWhisper = modelId.StartsWith("whisper-", StringComparison.OrdinalIgnoreCase);
        if (!isWhisper)
        {
            var cuda = string.Equals(Device, "cuda", StringComparison.OrdinalIgnoreCase);
            await _env.InstallAsync(["torch"], cuda ? "https://download.pytorch.org/whl/cu121" : null, progress, ct);
        }

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

        // Abhängigkeiten nur einmal installieren (Marker), nicht bei jedem Tausch.
        var depsMarker = Path.Combine(repo, ".audiola_deps_ok");
        if (!File.Exists(depsMarker))
        {
            var cuda = string.Equals(Device, "cuda", StringComparison.OrdinalIgnoreCase);
            await _env.InstallAsync(["torch", "torchvision", "torchaudio"],
                cuda ? "https://download.pytorch.org/whl/cu121" : null, progress, ct);
            await _env.InstallRequirementsAsync(Path.Combine(repo, "requirements.txt"), progress, ct);
            await _env.InstallAsync(["hf_xet"], null, progress, ct); // schnellerer HF-Download, entfernt die Warnung
            try { File.WriteAllText(depsMarker, "ok"); } catch { }
        }
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
        await _env.InstallAsync(["torch", "torchvision", "torchaudio"], "https://download.pytorch.org/whl/cu121",
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
                ["vc", "--input", inputWav, "--speaker", speaker, "--device", Device, "--models-dir", ModelsDir, "--out", outWav],
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

    public async Task<IReadOnlyList<TranscriptSegment>> TranscribeAsync(string inputWav, string whisperModel, CancellationToken ct = default)
    {
        RequireScript();
        var (code, stdout, err) = await RunAsync(
            ["transcribe", "--input", inputWav, "--model", whisperModel, "--device", Device], null, ct);
        if (code != 0)
            throw new InvalidOperationException($"Transkription fehlgeschlagen: {Short(err)}");

        var parsed = JsonSerializer.Deserialize<TranscriptResponse>(ExtractJson(stdout), JsonOpts);
        return parsed?.Segments?.Select(s => new TranscriptSegment(s.Start, s.End, (s.Text ?? "").Trim())).ToList()
               ?? [];
    }

    private string ModelsDir => _settings.Current.VoiceModelsDirectory;
    private string Device => string.IsNullOrWhiteSpace(_settings.Current.VoiceDevice) ? "auto" : _settings.Current.VoiceDevice;

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
        var psi = new ProcessStartInfo
        {
            // Bevorzugt die verwaltete Umgebung (mit allen installierten Paketen), sonst System-Python.
            FileName = _env.Exists ? _env.PythonExe : _settings.Current.PythonPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add(ScriptPath);
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) { stderr.AppendLine(e.Data); progress?.Report(e.Data); } };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* schon beendet */ }
            throw;
        }
        return (process.ExitCode, stdout.ToString(), stderr.ToString());
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
