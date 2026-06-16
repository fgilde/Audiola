using System.Diagnostics;
using System.IO;
using System.Text;

namespace Audiola.Services;

/// <summary>
/// Verwaltete venv unter <c>%LocalAppData%/Audiola/pyenv</c>. Wird aus der vom Nutzer
/// hinterlegten Basis-Python (Einstellungen) erzeugt; Pakete werden dort hinein installiert,
/// ohne die System-Python zu verändern.
/// </summary>
public sealed class PythonEnvironmentService : IPythonEnvironment
{
    private readonly ISettingsService _settings;

    public PythonEnvironmentService(ISettingsService settings) => _settings = settings;

    private static string EnvDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Audiola", "pyenv");

    public string PythonExe => Path.Combine(EnvDir, "Scripts", "python.exe");

    public bool Exists => File.Exists(PythonExe);

    public async Task EnsureAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (Exists) return;

        progress?.Report("Erstelle isolierte Python-Umgebung …");
        var basePython = string.IsNullOrWhiteSpace(_settings.Current.PythonPath) ? "python" : _settings.Current.PythonPath;
        Directory.CreateDirectory(Path.GetDirectoryName(EnvDir)!);

        var (code, err) = await RunAsync(basePython, ["-m", "venv", EnvDir], progress, ct);
        if (code != 0 || !Exists)
            throw new InvalidOperationException(
                "Konnte keine Python-Umgebung anlegen. Bitte Python 3.10+ installieren und den Pfad in den " +
                $"Einstellungen setzen (Basis-Python: '{basePython}'). Details: {Short(err)}");

        progress?.Report("Aktualisiere pip …");
        await RunAsync(PythonExe, ["-m", "pip", "install", "--upgrade", "pip", "wheel"], progress, ct);
    }

    public async Task InstallAsync(IReadOnlyList<string> packages, string? indexUrl = null,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (packages.Count == 0) return;
        await EnsureAsync(progress, ct);

        var args = new List<string> { "-m", "pip", "install", "--upgrade" };
        if (!string.IsNullOrWhiteSpace(indexUrl)) { args.Add("--index-url"); args.Add(indexUrl); }
        args.AddRange(packages);

        progress?.Report($"Installiere: {string.Join(", ", packages)} …");
        var (code, err) = await RunAsync(PythonExe, [.. args], progress, ct);
        if (code != 0)
            throw new InvalidOperationException($"pip install fehlgeschlagen ({string.Join(", ", packages)}): {Short(err)}");
    }

    public async Task InstallRequirementsAsync(string requirementsFile, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!File.Exists(requirementsFile)) return;
        await EnsureAsync(progress, ct);
        progress?.Report("Installiere requirements …");
        var (code, err) = await RunAsync(PythonExe, ["-m", "pip", "install", "-r", requirementsFile], progress, ct);
        if (code != 0)
            throw new InvalidOperationException($"pip install -r fehlgeschlagen: {Short(err)}");
    }

    private static string Short(string s) => string.IsNullOrEmpty(s) ? "(keine Ausgabe)" : s.Length > 400 ? s[^400..] : s;

    private static async Task<(int Code, string Err)> RunAsync(string exe, string[] args, IProgress<string>? progress, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var err = new StringBuilder();
        p.OutputDataReceived += (_, e) => { if (e.Data is not null) progress?.Report(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data is not null) { err.AppendLine(e.Data); progress?.Report(e.Data); } };
        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        await p.WaitForExitAsync(ct);
        return (p.ExitCode, err.ToString());
    }
}
