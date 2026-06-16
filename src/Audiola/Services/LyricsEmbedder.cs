using System.IO;
using System.Text;

namespace Audiola.Services;

/// <summary>
/// Bettet ein Transkript (LRC) in eine exportierte Audiodatei ein: als Lyrics-Metatag
/// (MP3/M4A/FLAC via TagLib) und immer zusätzlich als <c>.lrc</c>-Datei daneben
/// (für WAV bzw. synchronisierte Lyrics in Playern).
/// </summary>
public static class LyricsEmbedder
{
    public static void Embed(string audioPath, string? lrc, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(lrc) || !File.Exists(audioPath)) return;

        // Synchronisierte LRC-Datei neben dem Export.
        try { File.WriteAllText(Path.ChangeExtension(audioPath, ".lrc"), lrc); } catch { /* egal */ }

        // Unsynchronisierter Lyrics-Tag (Formate ohne Tag-Unterstützung wie WAV werfen → ignorieren).
        try
        {
            using var file = TagLib.File.Create(audioPath);
            file.Tag.Lyrics = StripTimestamps(lrc);
            if (!string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(file.Tag.Title))
                file.Tag.Title = title;
            file.Save();
        }
        catch { /* Format ohne Lyrics-Tag — die .lrc-Datei reicht */ }
    }

    /// <summary>Entfernt LRC-Zeitstempel/Metazeilen für den (unsynchronisierten) Lyrics-Tag.</summary>
    private static string StripTimestamps(string lrc)
    {
        var sb = new StringBuilder();
        foreach (var raw in lrc.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var close = line.LastIndexOf(']');
            var text = close >= 0 && close + 1 <= line.Length ? line[(close + 1)..] : line;
            if (line.StartsWith("[ti:") || line.StartsWith("[ar:") || line.StartsWith("[by:")) continue;
            if (!string.IsNullOrWhiteSpace(text)) sb.AppendLine(text.Trim());
        }
        return sb.ToString().TrimEnd();
    }
}
