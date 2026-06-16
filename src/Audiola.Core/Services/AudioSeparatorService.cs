using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Audiola.Services;

/// <summary>
/// HQ-Trennung über das Python-Paket <c>audio-separator</c> im verwalteten venv
/// (gleiche Mechanik wie die lokale Voice-Engine; wird bei Bedarf automatisch installiert).
/// </summary>
public sealed class AudioSeparatorService : IAdvancedSeparationService
{
    private readonly ISettingsService _settings;
    private readonly IPythonEnvironment _env;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AudioSeparatorService(ISettingsService settings, IPythonEnvironment env)
    {
        _settings = settings;
        _env = env;
    }

    private string ScriptPath => Path.Combine(AppContext.BaseDirectory, "voicebox_engine.py");
    public bool ScriptAvailable => File.Exists(ScriptPath);

    public IReadOnlyList<SeparationModel> Models { get; } =
    [
        new("roformer", "RoFormer (Vocals/Instrumental)", "SOTA-Qualität für Gesang/Instrumental.",
            "model_bs_roformer_ep_317_sdr_12.9755.ckpt"),
        new("demucs6s", "Demucs 6 Stems", "Vocals, Drums, Bass, Gitarre, Klavier, Rest.",
            "htdemucs_6s.yaml"),
        new("karaoke", "Karaoke (Lead/Background)", "Trennt Lead- von Background-Vocals.",
            "mel_band_roformer_karaoke_aufr33_viperx_sdr_10.1956.ckpt"),
    ];

    public async Task<IReadOnlyList<SeparatedStem>> SeparateAsync(string inputFile, string modelFilename,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!ScriptAvailable)
            throw new InvalidOperationException("Trenn-Skript (voicebox_engine.py) fehlt.");

        await EnsureInstalledAsync(progress, ct);

        var outDir = Path.Combine(Path.GetTempPath(), "Audiola", "hqsep", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outDir);

        var (code, stdout, err) = await RunAsync(
            ["separate", "--input", inputFile, "--out-dir", outDir, "--model", modelFilename], progress, ct);
        if (code != 0)
            throw new InvalidOperationException($"HQ-Trennung fehlgeschlagen: {Short(err)}");

        var parsed = JsonSerializer.Deserialize<SepResponse>(ExtractJson(stdout), JsonOpts);
        return parsed?.Files?
            .Where(f => !string.IsNullOrEmpty(f.Path) && File.Exists(f.Path))
            .Select(f => new SeparatedStem(f.Path!, Pretty(f.Stem ?? "Stem")))
            .ToList() ?? [];
    }

    private async Task EnsureInstalledAsync(IProgress<string>? progress, CancellationToken ct)
    {
        await _env.EnsureAsync(progress, ct);
        var marker = Path.Combine(_settings.Current.VoiceModelsDirectory, ".audio_separator_ok");
        if (File.Exists(marker)) return;

        Directory.CreateDirectory(_settings.Current.VoiceModelsDirectory);
        var cuda = string.Equals(_settings.Current.VoiceDevice, "cuda", StringComparison.OrdinalIgnoreCase);
        progress?.Report("Installiere audio-separator … (einmalig, groß)");
        await _env.InstallAsync([cuda ? "audio-separator[gpu]" : "audio-separator[cpu]"], null, progress, ct);
        try { File.WriteAllText(marker, "ok"); } catch { }
    }

    private static string Pretty(string stem) => stem.Trim() switch
    {
        "Vocals" => "Vocals", "Instrumental" => "Instrumental", "Drums" => "Drums", "Bass" => "Bass",
        "Guitar" => "Gitarre", "Piano" => "Klavier", "Other" => "Rest",
        "Lead Vocals" or "lead_vocals" => "Lead-Vocals",
        "Back Vocals" or "Backing Vocals" or "back_vocals" => "Background-Vocals",
        _ => stem.Trim()
    };

    private string Device => string.IsNullOrWhiteSpace(_settings.Current.VoiceDevice) ? "auto" : _settings.Current.VoiceDevice;

    private static string ExtractJson(string s)
    {
        var a = s.IndexOf('{'); var b = s.LastIndexOf('}');
        return a >= 0 && b > a ? s[a..(b + 1)] : "{}";
    }

    private static string Short(string s) => string.IsNullOrEmpty(s) ? "(keine Ausgabe)" : s.Length > 800 ? s[^800..] : s;

    private async Task<(int Code, string Stdout, string Stderr)> RunAsync(string[] args, IProgress<string>? progress, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _env.Exists ? _env.PythonExe : _settings.Current.PythonPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add(ScriptPath);
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var so = new StringBuilder();
        var se = new StringBuilder();
        p.OutputDataReceived += (_, e) => { if (e.Data is not null) so.AppendLine(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data is not null) { se.AppendLine(e.Data); progress?.Report(e.Data); } };
        p.Start(); p.BeginOutputReadLine(); p.BeginErrorReadLine();
        await p.WaitForExitAsync(ct);
        return (p.ExitCode, so.ToString(), se.ToString());
    }

    private sealed class SepResponse { public List<SepFile>? Files { get; set; } }
    private sealed class SepFile { public string? Path { get; set; } public string? Stem { get; set; } }
}
