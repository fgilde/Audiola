using System.Globalization;
using System.Text;

namespace Audiola.Services;

/// <summary>Erzeugt LRC-Text (zeitgestempelte Lyrics) aus Transkript-Segmenten.</summary>
public static class LrcWriter
{
    public static string ToLrc(IEnumerable<TranscriptSegment> segments, string? title = null, string? artist = null)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(title)) sb.AppendLine($"[ti:{title}]");
        if (!string.IsNullOrWhiteSpace(artist)) sb.AppendLine($"[ar:{artist}]");
        sb.AppendLine("[by:Audiola]");
        foreach (var s in segments)
        {
            if (string.IsNullOrWhiteSpace(s.Text)) continue;
            sb.Append('[').Append(Stamp(s.Start)).Append(']').AppendLine(s.Text.Trim());
        }
        return sb.ToString();
    }

    /// <summary>Reiner Text (alle Segmente zeilenweise) — z. B. für einen Lyrics-Metatag.</summary>
    public static string ToPlainText(IEnumerable<TranscriptSegment> segments) =>
        string.Join("\n", segments.Select(s => s.Text.Trim()).Where(t => t.Length > 0));

    private static string Stamp(double seconds)
    {
        if (seconds < 0) seconds = 0;
        var m = (int)(seconds / 60);
        var s = seconds - m * 60;
        return string.Create(CultureInfo.InvariantCulture, $"{m:00}:{s:00.00}");
    }
}
