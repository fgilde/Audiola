namespace Audiola.Services;

/// <summary>Ein Dateityp-Filter, den jeder Host in seine eigene Dialog-API übersetzt.</summary>
/// <param name="Name">Angezeigter Name, z. B. "Audiodateien".</param>
/// <param name="Extensions">Endungen ohne Punkt, z. B. ["mp3", "wav"]. Leer = alle Dateien.</param>
public sealed record FileFilter(string Name, params string[] Extensions)
{
    public static readonly FileFilter Audio = new("Audiodateien", "mp3", "wav", "flac", "m4a", "aac", "ogg", "aiff", "wma");
    public static readonly FileFilter Project = new("Audiola-Projekt", "audiola");
    public static readonly FileFilter Wav = new("WAV-Datei", "wav");
    public static readonly FileFilter Lyrics = new("Liedtext", "lrc", "txt");
    public static readonly FileFilter Image = new("Bilddateien", "jpg", "jpeg", "png", "bmp", "webp");
    public static readonly FileFilter Any = new("Alle Dateien");
}

/// <summary>
/// Datei- und Ordnerauswahl ohne Host-Bindung. Die ViewModels kannten vorher
/// <c>Microsoft.Win32.OpenFileDialog</c> direkt; das ging nur unter Windows.
/// </summary>
public interface IFileDialogs
{
    /// <summary>Öffnet eine oder (bei <paramref name="allowMultiple"/>) mehrere Dateien. Leer = abgebrochen.</summary>
    Task<IReadOnlyList<string>> OpenFilesAsync(string title, bool allowMultiple = false, params FileFilter[] filters);

    /// <summary>Zielpfad zum Speichern. <c>null</c> = abgebrochen.</summary>
    Task<string?> SaveFileAsync(string title, string? suggestedFileName = null, params FileFilter[] filters);

    /// <summary>Ordnerauswahl. <c>null</c> = abgebrochen.</summary>
    Task<string?> PickFolderAsync(string title);
}

/// <summary>Bequeme Einzeldatei-Variante (die häufigste Verwendung).</summary>
public static class FileDialogExtensions
{
    public static async Task<string?> OpenFileAsync(this IFileDialogs dialogs, string title, params FileFilter[] filters)
        => (await dialogs.OpenFilesAsync(title, false, filters)).FirstOrDefault();
}
