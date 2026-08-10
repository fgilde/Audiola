using System.IO;
using Audiola.Models;

namespace Audiola.Services;

/// <summary>Liest und schreibt Song-Metadaten (ID3/Vorbis/MP4-Tags inkl. Cover &amp; Lyrics) via TagLib.</summary>
public interface IMetadataService
{
    /// <summary>Liest alle verfügbaren Tags aus einer Audiodatei (leeres Objekt, falls keine).</summary>
    AudioMetadata Read(string path);

    /// <summary>
    /// Schreibt die Metadaten in die (bereits exportierte) Audiodatei. Bei <paramref name="embedLyrics"/>
    /// werden zusätzlich der Lyrics-Tag und eine <c>.lrc</c>-Sidecar-Datei geschrieben. Kann bei
    /// Formaten ohne Tag-Unterstützung eine Ausnahme werfen — der Aufrufer behandelt das als Warnung.
    /// </summary>
    void Write(string path, AudioMetadata meta, bool embedLyrics = true);
}

public sealed class MetadataService : IMetadataService
{
    private static readonly HashSet<string> TaggableExt = new(StringComparer.OrdinalIgnoreCase)
        { ".mp3", ".m4a", ".aac", ".flac", ".ogg", ".wma", ".aiff", ".aif", ".wav" };

    /// <summary>Formate mit voller Tag-Unterstützung (WAV bewusst ausgenommen — nur eingeschränkt).</summary>
    public static bool SupportsFullTags(string path)
    {
        var ext = Path.GetExtension(path);
        return TaggableExt.Contains(ext) && !ext.Equals(".wav", StringComparison.OrdinalIgnoreCase);
    }

    public AudioMetadata Read(string path)
    {
        var meta = new AudioMetadata();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return meta;
        try
        {
            using var f = TagLib.File.Create(path);
            var t = f.Tag;
            meta.Title = NullIfEmpty(t.Title);
            meta.Artist = NullIfEmpty(t.JoinedPerformers);
            meta.Album = NullIfEmpty(t.Album);
            meta.AlbumArtist = NullIfEmpty(t.JoinedAlbumArtists);
            meta.Genre = NullIfEmpty(t.JoinedGenres);
            meta.Year = t.Year;
            meta.TrackNumber = t.Track;
            meta.Comment = NullIfEmpty(t.Comment);
            meta.Lyrics = NullIfEmpty(t.Lyrics);

            var pic = t.Pictures.FirstOrDefault();
            if (pic?.Data?.Data is { Length: > 0 } data)
            {
                meta.CoverArt = data;
                meta.CoverMimeType = NullIfEmpty(pic.MimeType) ?? "image/jpeg";
            }
        }
        catch { /* Format ohne Tags / unlesbar → leere Metadaten */ }
        return meta;
    }

    public void Write(string path, AudioMetadata meta, bool embedLyrics = true)
    {
        if (!File.Exists(path)) return;

        // Synchronisierte LRC-Datei daneben (für WAV bzw. LRC-fähige Player).
        if (embedLyrics && !string.IsNullOrWhiteSpace(meta.Lyrics) && LooksLikeLrc(meta.Lyrics!))
        {
            try { File.WriteAllText(Path.ChangeExtension(path, ".lrc"), meta.Lyrics); } catch { /* egal */ }
        }

        using var f = TagLib.File.Create(path);
        var t = f.Tag;
        if (!string.IsNullOrWhiteSpace(meta.Title)) t.Title = meta.Title;
        if (!string.IsNullOrWhiteSpace(meta.Artist)) t.Performers = Split(meta.Artist!);
        if (!string.IsNullOrWhiteSpace(meta.Album)) t.Album = meta.Album;
        if (!string.IsNullOrWhiteSpace(meta.AlbumArtist)) t.AlbumArtists = Split(meta.AlbumArtist!);
        if (!string.IsNullOrWhiteSpace(meta.Genre)) t.Genres = Split(meta.Genre!);
        if (meta.Year > 0) t.Year = meta.Year;
        if (meta.TrackNumber > 0) t.Track = meta.TrackNumber;
        if (!string.IsNullOrWhiteSpace(meta.Comment)) t.Comment = meta.Comment;
        // Liedtext MIT LRC-Zeitstempeln einbetten, damit kompatible Player (z. B. AuralizeBlazor)
        // ihn synchron zur Wiedergabe anzeigen. Zeitstempel zu entfernen würde alles als eine Zeile
        // bei 0:00 darstellen ("nur Text sichtbar").
        if (embedLyrics && !string.IsNullOrWhiteSpace(meta.Lyrics)) t.Lyrics = meta.Lyrics!.Trim();

        if (meta.HasCover)
        {
            var pic = new TagLib.Picture(new TagLib.ByteVector(meta.CoverArt))
            {
                Type = TagLib.PictureType.FrontCover,
                MimeType = string.IsNullOrWhiteSpace(meta.CoverMimeType) ? "image/jpeg" : meta.CoverMimeType,
                Description = "Cover"
            };
            t.Pictures = [pic];
        }

        f.Save();
    }

    private static string[] Split(string value) =>
        value.Split([';', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool LooksLikeLrc(string text) =>
        System.Text.RegularExpressions.Regex.IsMatch(text, @"\[\d{1,2}:\d{2}");
}
