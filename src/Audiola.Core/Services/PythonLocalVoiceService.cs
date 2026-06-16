using System.Diagnostics;
using System.IO;
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

    private static IReadOnlyList<string> PackagesFor(string modelId) => modelId switch
    {
        "qwen3-tts" => ["qwen-tts", "soundfile", "numpy"],
        "xtts-v2" => ["TTS", "soundfile", "numpy"],
        "kokoro" => ["kokoro", "soundfile", "numpy"],
        "chatterbox" => ["chatterbox-tts", "soundfile", "numpy"],
        _ when modelId.StartsWith("whisper-", StringComparison.OrdinalIgnoreCase) => ["faster-whisper"],
        _ => []
    };

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

    public async Task<(float[] Samples, int SampleRate)> ChangeVoiceAsync(string inputWav, VoiceProfile profile, CancellationToken ct = default)
    {
        RequireScript();
        var speaker = profile.SamplePaths.FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException("Diese lokale Stimme hat kein Referenz-Sample für den Stimmtausch.");
        var outWav = TempWav("vc");
        var (code, _, err) = await RunAsync(
            ["vc", "--input", inputWav, "--speaker", speaker, "--device", Device, "--models-dir", ModelsDir, "--out", outWav],
            null, ct);
        if (code != 0 || !File.Exists(outWav))
            throw new InvalidOperationException($"Lokaler Stimmtausch fehlgeschlagen: {Short(err)}");
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
        await process.WaitForExitAsync(ct);
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
    private sealed class TranscriptResponse { public List<SegmentDto>? Segments { get; set; } }
    private sealed class SegmentDto { public double Start { get; set; } public double End { get; set; } public string? Text { get; set; } }
}
