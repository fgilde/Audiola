namespace Audiola.Models;

/// <summary>
/// Song-Metadaten (ID3-/Tag-Felder) für das gesamte Projekt bzw. eine einzelne Audiodatei.
/// Reines, serialisierbares POCO — wird sowohl im Projekt (.audiola) gespeichert als auch
/// vom Tag-Editor und beim Export verwendet. Cover-Art liegt als Rohbytes vor (JSON → base64).
/// </summary>
public sealed class AudioMetadata
{
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? AlbumArtist { get; set; }
    public string? Genre { get; set; }

    /// <summary>Erscheinungsjahr (0 = nicht gesetzt).</summary>
    public uint Year { get; set; }

    /// <summary>Titelnummer (0 = nicht gesetzt).</summary>
    public uint TrackNumber { get; set; }

    public string? Comment { get; set; }

    /// <summary>Liedtext — bevorzugt als LRC (zeitgestempelt); für den Tag werden Zeitstempel entfernt.</summary>
    public string? Lyrics { get; set; }

    /// <summary>Cover-Art als Rohbytes (z. B. JPEG/PNG), null = kein Cover.</summary>
    public byte[]? CoverArt { get; set; }

    /// <summary>MIME-Typ der Cover-Art (z. B. image/jpeg).</summary>
    public string? CoverMimeType { get; set; }

    public bool HasCover => CoverArt is { Length: > 0 };

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(Artist) &&
        string.IsNullOrWhiteSpace(Album) && string.IsNullOrWhiteSpace(AlbumArtist) &&
        string.IsNullOrWhiteSpace(Genre) && string.IsNullOrWhiteSpace(Comment) &&
        string.IsNullOrWhiteSpace(Lyrics) && Year == 0 && TrackNumber == 0 && !HasCover;

    public AudioMetadata Clone() => new()
    {
        Title = Title,
        Artist = Artist,
        Album = Album,
        AlbumArtist = AlbumArtist,
        Genre = Genre,
        Year = Year,
        TrackNumber = TrackNumber,
        Comment = Comment,
        Lyrics = Lyrics,
        CoverArt = CoverArt is null ? null : (byte[])CoverArt.Clone(),
        CoverMimeType = CoverMimeType
    };
}
