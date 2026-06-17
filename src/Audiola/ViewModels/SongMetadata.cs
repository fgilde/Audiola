using Audiola.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Audiola.ViewModels;

/// <summary>
/// Projektweiter, beobachtbarer Song-Metadaten-Zustand. Wird vom Tag-Editor bearbeitet,
/// beim Öffnen einer Datei befüllt, im Projekt (.audiola) gespeichert/geladen und beim
/// Export als Vorlage übernommen. Jahr/Titelnummer als Text (leer = nicht gesetzt).
/// </summary>
public sealed partial class SongMetadata : ObservableObject
{
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _artist = "";
    [ObservableProperty] private string _album = "";
    [ObservableProperty] private string _albumArtist = "";
    [ObservableProperty] private string _genre = "";
    [ObservableProperty] private string _year = "";
    [ObservableProperty] private string _trackNumber = "";
    [ObservableProperty] private string _comment = "";
    [ObservableProperty] private string _lyrics = "";

    [ObservableProperty] private byte[]? _coverArt;
    [ObservableProperty] private string? _coverMimeType;

    public bool HasCover => CoverArt is { Length: > 0 };
    partial void OnCoverArtChanged(byte[]? value) => OnPropertyChanged(nameof(HasCover));

    public bool HasLyrics => !string.IsNullOrWhiteSpace(Lyrics);
    partial void OnLyricsChanged(string value) => OnPropertyChanged(nameof(HasLyrics));

    /// <summary>Übernimmt Werte aus einem POCO (z. B. aus Datei-Tags oder Projekt). Leere Felder überschreiben nichts, wenn <paramref name="onlyFillEmpty"/>.</summary>
    public void Apply(AudioMetadata? m, bool onlyFillEmpty = false)
    {
        if (m is null) return;
        void Set(string cur, string? val, Action<string> set)
        {
            if (string.IsNullOrWhiteSpace(val)) return;
            if (onlyFillEmpty && !string.IsNullOrWhiteSpace(cur)) return;
            set(val.Trim());
        }
        Set(Title, m.Title, v => Title = v);
        Set(Artist, m.Artist, v => Artist = v);
        Set(Album, m.Album, v => Album = v);
        Set(AlbumArtist, m.AlbumArtist, v => AlbumArtist = v);
        Set(Genre, m.Genre, v => Genre = v);
        Set(Year, m.Year > 0 ? m.Year.ToString() : null, v => Year = v);
        Set(TrackNumber, m.TrackNumber > 0 ? m.TrackNumber.ToString() : null, v => TrackNumber = v);
        Set(Comment, m.Comment, v => Comment = v);
        Set(Lyrics, m.Lyrics, v => Lyrics = v);
        if (m.HasCover && (!onlyFillEmpty || !HasCover))
        {
            CoverArt = m.CoverArt;
            CoverMimeType = m.CoverMimeType;
        }
    }

    /// <summary>Erzeugt ein serialisierbares POCO aus dem aktuellen Zustand.</summary>
    public AudioMetadata ToMetadata() => new()
    {
        Title = NullIfEmpty(Title),
        Artist = NullIfEmpty(Artist),
        Album = NullIfEmpty(Album),
        AlbumArtist = NullIfEmpty(AlbumArtist),
        Genre = NullIfEmpty(Genre),
        Year = uint.TryParse(Year, out var y) ? y : 0,
        TrackNumber = uint.TryParse(TrackNumber, out var tn) ? tn : 0,
        Comment = NullIfEmpty(Comment),
        Lyrics = NullIfEmpty(Lyrics),
        CoverArt = CoverArt,
        CoverMimeType = CoverMimeType
    };

    public void Clear()
    {
        Title = Artist = Album = AlbumArtist = Genre = Year = TrackNumber = Comment = Lyrics = "";
        CoverArt = null;
        CoverMimeType = null;
    }

    public bool IsEmpty => ToMetadata().IsEmpty;

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
