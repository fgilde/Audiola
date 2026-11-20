using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Audiola.Models;
using NAudio.Wave;
using Audiola.Services.Audio;

namespace Audiola.Services;

/// <summary>
/// Liest und erklärt Provenienz-/Herkunftsdaten einer Audiodatei.
/// Kombiniert einen nativen Marker-Scan (C2PA/JUMBF, XMP, ID3, Encoder-Spuren)
/// mit optionalem <c>c2patool</c> für die vollständige C2PA-Manifest-Auswertung.
/// Es wird nichts verändert — reine Analyse.
/// </summary>
public sealed class ProvenanceService : IProvenanceService
{
    // (ASCII-Muster, Kategorie, Titel, Erklärung, Schwere, WortGrenze)
    // WortGrenze=true für kurze Markennamen, die sonst als Teilstring falsch treffen
    // (z. B. "Udio" in "audio").
    private static readonly (string Pattern, string Category, string Title, string Detail, FindingSeverity Sev, bool WholeWord)[] Markers =
    [
        ("jumbf", "C2PA", L.T("JUMBF-Box gefunden"),
            L.T("Container für eingebettete C2PA-Manifeste (Content Credentials)."), FindingSeverity.AiIndicator, false),
        ("c2pa.actions", "C2PA", L.T("C2PA-Aktionsliste"),
            L.T("Dokumentierte Bearbeitungsschritte; enthält oft den Hinweis auf KI-Erzeugung."), FindingSeverity.AiIndicator, false),
        ("c2pa", "C2PA", L.T("C2PA-Kennung gefunden"),
            L.T("Die Datei enthält ein C2PA-Manifest (kryptografisch signierte Herkunftskette)."), FindingSeverity.AiIndicator, false),
        ("compositeWithTrainedAlgorithmicMedia", L.T("Herkunft"), L.T("digitalSourceType: composite (KI-Anteil)"),
            L.T("IPTC-Kennung 'teils mit KI erzeugt/bearbeitet'."), FindingSeverity.AiIndicator, false),
        ("trainedAlgorithmicMedia", L.T("Herkunft"), L.T("digitalSourceType: trainedAlgorithmicMedia"),
            L.T("IPTC-Kennung 'mit KI erzeugt'. Wird von Scannern direkt als KI-Signal gewertet."), FindingSeverity.AiIndicator, false),
        ("digitalSourceType", L.T("Herkunft"), L.T("digitalSourceType-Feld"),
            L.T("Feld zur Kennzeichnung der Erzeugungsart (KI/Foto/…)."), FindingSeverity.AiIndicator, false),
        ("http://ns.adobe.com/xap/1.0/", "XMP", L.T("XMP-Metadaten"),
            L.T("Adobe-XMP-Block; kann Urheber-, Tool- und Herkunftsangaben enthalten."), FindingSeverity.Info, false),
        ("contentauth", "C2PA", L.T("Content-Authenticity-Hinweis"),
            L.T("Verweis auf die Content Authenticity Initiative / Content Credentials."), FindingSeverity.AiIndicator, false),
        ("Suno", L.T("Herkunft"), L.T("Generator-Spur: Suno"),
            L.T("Hinweis auf den KI-Musikgenerator Suno in den Metadaten."), FindingSeverity.AiIndicator, true),
        ("Udio", L.T("Herkunft"), L.T("Generator-Spur: Udio"),
            L.T("Hinweis auf den KI-Musikgenerator Udio in den Metadaten."), FindingSeverity.AiIndicator, true),
    ];

    public Task<ProvenanceReport> AnalyzeAsync(string filePath, CancellationToken ct = default)
        => Task.Run(() => Analyze(filePath, ct), ct);

    private static ProvenanceReport Analyze(string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException(L.T("Datei nicht gefunden."), filePath);

        var findings = new List<Finding>();

        // 1) Formatinfo.
        try
        {
            using var reader = PortableAudioFile.Open(filePath);
            var wf = reader.WaveFormat;
            findings.Add(new Finding("Format",
                $"{Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant()}, " +
                L.F("{0} Hz, {1} Kanäle, {2:mm\\:ss}", wf.SampleRate, wf.Channels, reader.TotalTime),
                L.F("Dateigröße: {0} KB.", new FileInfo(filePath).Length / 1024),
                FindingSeverity.Info));
        }
        catch
        {
            findings.Add(new Finding("Format", L.T("Format nicht lesbar"),
                L.T("Die Datei konnte nicht als Audio dekodiert werden."), FindingSeverity.Notice));
        }

        // 2) Nativer Marker-Scan (Kopf + Ende der Datei).
        var bytes = ReadHeadAndTail(filePath, 4 * 1024 * 1024, 2 * 1024 * 1024);
        var text = Encoding.ASCII.GetString(bytes);

        if (bytes.Length >= 3 && bytes[0] == (byte)'I' && bytes[1] == (byte)'D' && bytes[2] == (byte)'3')
            findings.Add(new Finding("ID3", L.T("ID3-Tag vorhanden"),
                L.T("MP3-Metadatenblock (Titel, Künstler, Kommentar, Encoder)."), FindingSeverity.Info));

        var seen = new HashSet<string>();
        foreach (var m in Markers)
        {
            ct.ThrowIfCancellationRequested();
            var match = m.WholeWord
                ? Regex.IsMatch(text, $@"\b{Regex.Escape(m.Pattern)}\b", RegexOptions.IgnoreCase)
                : text.Contains(m.Pattern, StringComparison.OrdinalIgnoreCase);
            if (match && seen.Add(m.Title))
                findings.Add(new Finding(m.Category, m.Title, m.Detail, m.Sev));
        }

        // 3) Optional: vollständige C2PA-Auswertung via c2patool.
        var (c2paAvailable, c2paRaw) = TryRunC2paTool(filePath);
        if (c2paAvailable && !string.IsNullOrWhiteSpace(c2paRaw))
        {
            var hasManifest = !c2paRaw.Contains("No claim", StringComparison.OrdinalIgnoreCase)
                              && c2paRaw.Contains("manifest", StringComparison.OrdinalIgnoreCase);
            findings.Add(new Finding("C2PA",
                hasManifest ? L.T("c2patool: gültiges Manifest gelesen") : L.T("c2patool: kein Manifest gefunden"),
                hasManifest
                    ? L.T("Vollständige Herkunftskette unten einsehbar.")
                    : L.T("Keine eingebettete C2PA-Herkunftskette in der Datei."),
                hasManifest ? FindingSeverity.AiIndicator : FindingSeverity.Info));
        }

        var assessment = BuildAssessment(findings, c2paAvailable);

        return new ProvenanceReport
        {
            FilePath = filePath,
            Findings = findings,
            Assessment = assessment,
            C2paToolAvailable = c2paAvailable,
            C2paRaw = c2paRaw
        };
    }

    private static string BuildAssessment(IReadOnlyList<Finding> findings, bool c2paAvailable)
    {
        var aiIndicators = findings.Where(f => f.Severity == FindingSeverity.AiIndicator).ToList();
        var sb = new StringBuilder();

        if (aiIndicators.Count > 0)
        {
            sb.AppendLine(L.T("➤ Es wurden explizite Herkunfts-/KI-Kennzeichnungen auf Metadatenebene gefunden:"));
            foreach (var f in aiIndicators)
                sb.AppendLine($"   • {f.Title}");
            sb.AppendLine();
            sb.AppendLine(L.T("Solche Marker (C2PA-Manifeste, digitalSourceType, Generator-Spuren) werden von"));
            sb.AppendLine(L.T("Auto-Scannern in der Regel direkt ausgewertet — das erklärt eine KI-Einstufung"));
            sb.AppendLine(L.T("bereits auf der Metadaten-Stufe."));
        }
        else
        {
            sb.AppendLine(L.T("➤ Auf Metadatenebene wurden keine Provenienz-/Wasserzeichen-Marker gefunden."));
            sb.AppendLine();
            sb.AppendLine(L.T("Wichtig: Das ist KEIN Beleg, dass der Track nicht als KI erkannt wird."));
        }

        sb.AppendLine();
        sb.AppendLine(L.T("Warum ein Track trotzdem als KI erkannt werden kann (mehrstufige Detektoren):"));
        sb.AppendLine(L.T("  1. Metadaten-/Provenienz-Stufe — liest C2PA/XMP/ID3 (hier analysiert)."));
        sb.AppendLine(L.T("  2. Audio-Wasserzeichen-Stufe — z. B. SynthID-artige, unhörbare Signale direkt"));
        sb.AppendLine(L.T("     im Audiosignal. Diese stehen NICHT in den Metadaten und sind nur mit dem"));
        sb.AppendLine(L.T("     Detektor des Anbieters nachweisbar — von außen also nicht sichtbar."));
        sb.AppendLine(L.T("  3. Klassifikator-/Forensik-Stufe — ein ML-Modell bewertet das Audio selbst"));
        sb.AppendLine(L.T("     (Artefakte, Spektrum, Phase) und braucht gar keine eingebetteten Marker."));
        sb.AppendLine();
        sb.AppendLine(L.T("Für eine belastbare Aussage, WELCHE Stufe anschlägt, lässt sich das von außen nicht"));
        sb.AppendLine(L.T("trennen — sinnvoll ist, beim Anbieter die Einzelstufen-Auswertung bzw. die"));
        sb.AppendLine(L.T("Detektor-Scores pro Stufe anzufragen (z. B. Wasserzeichen-Score vs. Klassifikator-Score)."));

        if (!c2paAvailable)
        {
            sb.AppendLine();
            sb.AppendLine(L.T("Tipp: Für die vollständige C2PA-Manifest-Auswertung 'c2patool' installieren"));
            sb.AppendLine(L.T("(https://github.com/contentauth/c2patool) und im PATH verfügbar machen."));
        }

        return sb.ToString().TrimEnd();
    }

    private static byte[] ReadHeadAndTail(string path, int headBytes, int tailBytes)
    {
        using var fs = File.OpenRead(path);
        var len = fs.Length;

        if (len <= headBytes + tailBytes)
        {
            var all = new byte[len];
            fs.ReadExactly(all, 0, (int)len);
            return all;
        }

        var head = new byte[headBytes];
        fs.ReadExactly(head, 0, headBytes);

        var tail = new byte[tailBytes];
        fs.Seek(-tailBytes, SeekOrigin.End);
        fs.ReadExactly(tail, 0, tailBytes);

        var buffer = new byte[headBytes + tailBytes];
        Array.Copy(head, 0, buffer, 0, headBytes);
        Array.Copy(tail, 0, buffer, headBytes, tailBytes);
        return buffer;
    }

    private static (bool Available, string? Output) TryRunC2paTool(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "c2patool",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add(filePath);

            using var process = Process.Start(psi);
            if (process is null) return (false, null);

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(30_000);

            var output = string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
            return (true, output);
        }
        catch
        {
            // c2patool nicht installiert / nicht im PATH.
            return (false, null);
        }
    }
}
