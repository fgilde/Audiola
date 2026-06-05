using Audiola.Models;

namespace Audiola.Services;

/// <summary>
/// Lädt eine Audiodatei in die Sitzung (Wellenform + Player) und pflegt die
/// Liste der zuletzt geöffneten Dateien. Gemeinsame Lade-Logik für Datei-Dialog,
/// Drag &amp; Drop und „Zuletzt geöffnet“.
/// </summary>
public interface ITrackLoader
{
    IReadOnlyList<string> RecentFiles { get; }

    event EventHandler? RecentChanged;

    /// <summary>Lädt die Datei; wirft bei Fehler eine Exception.</summary>
    Task<AudioTrack> LoadAsync(string filePath, CancellationToken ct = default);
}
