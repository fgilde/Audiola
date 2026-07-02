using System.Diagnostics;
using System.IO;
using Audiola.Models;

namespace Audiola.Services;

/// <summary>
/// Stem-Trennung ueber das lokal installierte Demucs (Meta) via "python -m demucs".
///
/// Voraussetzung beim Anwender:
///   pip install -U demucs
///
/// Demucs legt die Ausgabe unter &lt;out&gt;/&lt;model&gt;/&lt;trackname&gt;/{vocals,drums,bass,other}.wav ab.
/// </summary>
public sealed class DemucsStemSeparationService : IStemSeparationService
{
    private readonly ISettingsService _settings;

    public DemucsStemSeparationService(ISettingsService settings)
    {
        _settings = settings;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var (exitCode, _) = await RunAsync(["-m", "demucs", "--help"], null, ct);
            return exitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<StemSet> SeparateAsync(
        string inputFile,
        IProgress<string>? progress = null,
        string? modelOverride = null,
        int shifts = 0,
        CancellationToken ct = default)
    {
        if (!File.Exists(inputFile))
            throw new FileNotFoundException("Eingabedatei nicht gefunden.", inputFile);

        var model = string.IsNullOrWhiteSpace(modelOverride) ? _settings.Current.DemucsModel : modelOverride;
        var outDir = _settings.Current.OutputDirectory;
        Directory.CreateDirectory(outDir);

        var argList = new List<string> { "-m", "demucs", "-n", model, "--out", outDir };
        if (shifts > 0)
        {
            // Sauberere Trennung: mehrfach versetzt rechnen + größere Überlappung.
            argList.Add("--shifts"); argList.Add(shifts.ToString(System.Globalization.CultureInfo.InvariantCulture));
            argList.Add("--overlap"); argList.Add("0.5");
        }
        argList.Add(inputFile);

        progress?.Report($"Starte Demucs ({model}{(shifts > 0 ? $", shifts={shifts}" : "")}) …");
        var (exitCode, _) = await RunAsync([.. argList], progress, ct);

        if (exitCode != 0)
            throw new InvalidOperationException(
                $"Demucs ist mit Code {exitCode} fehlgeschlagen. Ist 'pip install demucs' ausgefuehrt und der Python-Pfad korrekt?");

        var trackName = Path.GetFileNameWithoutExtension(inputFile);
        var stemDir = Path.Combine(outDir, model, trackName);

        if (!Directory.Exists(stemDir))
            throw new DirectoryNotFoundException($"Erwartetes Stem-Verzeichnis nicht gefunden: {stemDir}");

        var stems = new List<Stem>();
        foreach (var kind in Enum.GetValues<StemKind>())
        {
            var path = Path.Combine(stemDir, $"{kind.ToString().ToLowerInvariant()}.wav");
            if (File.Exists(path))
                stems.Add(new Stem { Kind = kind, FilePath = path });
        }

        if (stems.Count == 0)
            throw new InvalidOperationException("Demucs hat keine Stems erzeugt.");

        progress?.Report($"Fertig: {stems.Count} Stems extrahiert.");

        return new StemSet
        {
            SourceTrackPath = inputFile,
            Stems = stems,
            OutputDirectory = stemDir
        };
    }

    private async Task<(int ExitCode, string Output)> RunAsync(
        string[] args,
        IProgress<string>? progress,
        CancellationToken ct)
    {
        // Demucs schreibt Fortschritt nach stderr; stdout zeilenweise mitstreamen.
        var r = await ProcessRunner.RunAsync(_settings.Current.PythonPath, args, progress, ct,
            ProcessRunner.StdoutMode.StreamLines);
        return (r.ExitCode, r.Stdout + r.Stderr);
    }
}
