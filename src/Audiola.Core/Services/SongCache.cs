using System.Security.Cryptography;
using System.Text.Json;

namespace Audiola.Services;

/// <summary>Zwischengespeicherte Song-Daten (Lyrics, später Melodie) für die Karaoke-Wiederverwendung.</summary>
public sealed class SongCacheEntry
{
    public string? Title { get; set; }
    public string? Artist { get; set; }
    /// <summary>Synchronisierte Lyrics (LRC) — Herzstück des Caches.</summary>
    public string? Lrc { get; set; }
    /// <summary>Referenz-Melodie als JSON — non-null heißt „Analyse ist gelaufen" (auch wenn leer).</summary>
    public string? MelodyJson { get; set; }
    public double DurationSeconds { get; set; }
    /// <summary>Zuletzt bekannter Quellpfad (nur informativ — identifiziert wird über den Audio-Hash).</summary>
    public string? LastPath { get; set; }
    /// <summary>Persistierte spielbare WAV (gerenderter Projekt-Mix) — spart das Neu-Rendern komplett.</summary>
    public string? PlayableWav { get; set; }
}

/// <summary>
/// Inhalt-basierter Song-Cache: identifiziert Audiodateien über einen Hash der DATEIINHALTE
/// (nicht über den Pfad), sodass dieselbe MP3 an anderem Ort/Namen wiedererkannt wird und
/// Lyrics/Melodie nicht erneut extrahiert werden müssen. Ablage als JSON pro Song unter
/// %LOCALAPPDATA%\Audiola\songcache (von Audiola und Singola gemeinsam genutzt).
/// </summary>
public static class SongCache
{
    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Audiola", "songcache");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Schneller, stabiler Inhalts-Hash: SHA-256 über die ersten und letzten 2 MB plus Dateilänge.
    /// Bewusst nicht die ganze Datei (WAVs können riesig sein); für die Wiedererkennung reicht das.
    /// </summary>
    public static string ComputeAudioHash(string path)
    {
        const int chunk = 2 * 1024 * 1024;
        using var fs = File.OpenRead(path);
        using var sha = SHA256.Create();

        var buf = new byte[chunk];
        var read = fs.Read(buf, 0, chunk);
        sha.TransformBlock(buf, 0, read, null, 0);

        if (fs.Length > 2L * chunk)
        {
            fs.Seek(-chunk, SeekOrigin.End);
            read = fs.Read(buf, 0, chunk);
            sha.TransformBlock(buf, 0, read, null, 0);
        }

        var len = BitConverter.GetBytes(fs.Length);
        sha.TransformFinalBlock(len, 0, len.Length);
        return Convert.ToHexString(sha.Hash!);
    }

    /// <summary>Ablage-Pfad für die persistierte spielbare WAV eines Songs.</summary>
    public static string PlayableWavPath(string audioHash)
    {
        Directory.CreateDirectory(CacheDir);
        return Path.Combine(CacheDir, audioHash + ".wav");
    }

    public static SongCacheEntry? Load(string audioHash)
    {
        try
        {
            var file = Path.Combine(CacheDir, audioHash + ".json");
            if (!File.Exists(file)) return null;
            return JsonSerializer.Deserialize<SongCacheEntry>(File.ReadAllText(file));
        }
        catch { return null; }
    }

    public static void Save(string audioHash, SongCacheEntry entry)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            File.WriteAllText(Path.Combine(CacheDir, audioHash + ".json"),
                JsonSerializer.Serialize(entry, JsonOptions));
        }
        catch { /* Cache ist optional — Fehler nie fatal */ }
    }
}
