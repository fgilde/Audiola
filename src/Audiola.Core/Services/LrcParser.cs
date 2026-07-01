using System.Globalization;
using System.Text.RegularExpressions;

namespace Audiola.Services;

/// <summary>Eine Liedzeile mit Startzeit (s) aus einer LRC-Datei.</summary>
public readonly record struct LyricLine(double TimeSeconds, string Text);

/// <summary>
/// Parst LRC-Lyrics (<c>[mm:ss.xx] Text</c>) in zeitgestempelte Zeilen für die Karaoke-Anzeige.
/// Gegenstück zu <see cref="LrcWriter"/>. ID-Tags (<c>[ti:]</c>, <c>[ar:]</c> …) werden übersprungen.
/// </summary>
public static partial class LrcParser
{
    [GeneratedRegex(@"\[(\d{1,2}):(\d{1,2})(?:[.:](\d{1,3}))?\]")]
    private static partial Regex TimeTag();

    /// <summary>True, wenn der Text mindestens einen Zeitstempel enthält (also echtes LRC, kein Fließtext).</summary>
    public static bool HasTimestamps(string? lrc) => !string.IsNullOrWhiteSpace(lrc) && TimeTag().IsMatch(lrc);

    /// <summary>Parst LRC in nach Zeit sortierte Zeilen. Leere/reine Tag-Zeilen entfallen.</summary>
    public static IReadOnlyList<LyricLine> Parse(string? lrc)
    {
        var result = new List<LyricLine>();
        if (string.IsNullOrWhiteSpace(lrc)) return result;

        foreach (var raw in lrc.Replace("\r\n", "\n").Split('\n'))
        {
            var matches = TimeTag().Matches(raw);
            if (matches.Count == 0) continue;

            var text = TimeTag().Replace(raw, "").Trim();
            if (text.Length == 0) continue; // Zeile ohne Text (z. B. Zwischenraum) überspringen

            foreach (Match m in matches)
            {
                int min = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                int sec = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                double frac = 0;
                if (m.Groups[3].Success)
                {
                    var f = m.Groups[3].Value;
                    frac = int.Parse(f, CultureInfo.InvariantCulture) / Math.Pow(10, f.Length);
                }
                result.Add(new LyricLine(min * 60 + sec + frac, text));
            }
        }

        result.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));
        return result;
    }
}
