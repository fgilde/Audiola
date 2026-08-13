using System.Diagnostics;
using System.IO;
using System.Text;

namespace Audiola.Services;

/// <summary>
/// Verwaltete venv im plattformüblichen Audiola-Datenordner. Wird aus der vom Nutzer
/// hinterlegten Basis-Python (Einstellungen) erzeugt; Pakete werden dort hinein installiert,
/// ohne die System-Python zu verändern.
/// </summary>
public sealed class PythonEnvironmentService : IPythonEnvironment
{
    private readonly ISettingsService _settings;

    public PythonEnvironmentService(ISettingsService settings) => _settings = settings;

    private static string EnvDir => Path.Combine(AppPaths.LocalDataDirectory, "pyenv");

    public string PythonExe => AppPaths.PythonExecutable(EnvDir);

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
        IProgress<string>? progress = null, CancellationToken ct = default, bool forceReinstall = false)
    {
        if (packages.Count == 0) return;
        await EnsureAsync(progress, ct);

        var args = new List<string> { "-m", "pip", "install", "--upgrade" };
        if (forceReinstall) args.Add("--force-reinstall");
        if (!string.IsNullOrWhiteSpace(indexUrl)) { args.Add("--index-url"); args.Add(indexUrl); }
        args.AddRange(packages);

        progress?.Report($"Installiere: {string.Join(", ", packages)} …");
        var (code, err) = await RunAsync(PythonExe, [.. args], progress, ct);
        if (code != 0)
            throw new InvalidOperationException($"pip install fehlgeschlagen ({string.Join(", ", packages)}): {Short(err)}");
    }

    public async Task UninstallAsync(IReadOnlyList<string> packages, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (packages.Count == 0 || !Exists) return;
        progress?.Report($"Entferne: {string.Join(", ", packages)} …");
        var (code, err) = await RunAsync(PythonExe, ["-m", "pip", "uninstall", "-y", .. packages], progress, ct);
        // Ein nicht installiertes Paket ist kein Fehler — pip meldet das nur.
        if (code != 0) progress?.Report($"pip uninstall meldete: {Short(err)}");
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
        var r = await ProcessRunner.RunAsync(exe, args, progress, ct, ProcessRunner.StdoutMode.StreamLines);
        return (r.ExitCode, r.Stderr);
    }
}
