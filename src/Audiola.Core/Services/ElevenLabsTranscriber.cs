using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Audiola.Services;

/// <summary>
/// Eigenständige Song-Transkription über ElevenLabs Scribe (speech-to-text) → LRC.
/// UI-frei und ohne weitere Abhängigkeiten, damit auch Singola (Karaoke) sie nutzen kann.
/// Wörter werden anhand von Pausen/Zeilenlänge zu Karaoke-Zeilen gruppiert.
/// </summary>
public static class ElevenLabsTranscriber
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };

    /// <summary>Transkribiert die Audiodatei und liefert synchronisierte LRC-Lyrics (oder null ohne Wörter).</summary>
    public static async Task<string?> TranscribeToLrcAsync(string audioPath, string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("Kein ElevenLabs-API-Key hinterlegt.");
        if (!File.Exists(audioPath)) throw new FileNotFoundException("Audiodatei nicht gefunden.", audioPath);

        var bytes = await File.ReadAllBytesAsync(audioPath, ct);
        using var form = new MultipartFormDataContent();
        var fc = new ByteArrayContent(bytes);
        fc.Headers.ContentType = new MediaTypeHeaderValue(MimeForExtension(audioPath));
        form.Add(fc, "file", Path.GetFileName(audioPath));
        form.Add(new StringContent("scribe_v1"), "model_id");

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.elevenlabs.io/v1/speech-to-text") { Content = form };
        req.Headers.Add("xi-api-key", apiKey);
        using var resp = await Http.SendAsync(req, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Transkription fehlgeschlagen ({(int)resp.StatusCode}): {Truncate(json, 300)}");

        return WordsToLrc(json);
    }

    /// <summary>Gruppiert die Scribe-Wortliste zu LRC-Zeilen (neue Zeile bei Gesangspause oder Zeilenlänge).</summary>
    internal static string? WordsToLrc(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        if (!doc.RootElement.TryGetProperty("words", out var words)) return null;

        var sb = new StringBuilder();
        var line = new StringBuilder();
        double lineStart = -1, lastEnd = 0;

        void Flush()
        {
            var text = line.ToString().Trim();
            if (text.Length > 0 && lineStart >= 0)
                sb.AppendLine($"[{(int)(lineStart / 60):00}:{lineStart % 60:00.00}]{text}".Replace(',', '.'));
            line.Clear();
            lineStart = -1;
        }

        foreach (var w in words.EnumerateArray())
        {
            if (w.TryGetProperty("type", out var t) && t.GetString() != "word") continue;
            var text = w.TryGetProperty("text", out var txt) ? txt.GetString() ?? "" : "";
            if (text.Length == 0) continue;
            var start = w.TryGetProperty("start", out var s) ? s.GetDouble() : lastEnd;
            var end = w.TryGetProperty("end", out var e) ? e.GetDouble() : start;

            // Neue Zeile bei deutlicher Pause oder wenn die Zeile lang genug ist.
            if (line.Length > 0 && (start - lastEnd >= 1.1 || line.Length >= 45)) Flush();
            if (lineStart < 0) lineStart = start;
            if (line.Length > 0) line.Append(' ');
            line.Append(text.Trim());
            lastEnd = end;
        }
        Flush();

        return sb.Length > 0 ? sb.ToString() : null;
    }

    private static string MimeForExtension(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".mp3" => "audio/mpeg",
        ".wav" => "audio/wav",
        ".ogg" => "audio/ogg",
        ".flac" => "audio/flac",
        ".m4a" or ".aac" or ".mp4" => "audio/mp4",
        _ => "application/octet-stream",
    };

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n] + "…";
}
