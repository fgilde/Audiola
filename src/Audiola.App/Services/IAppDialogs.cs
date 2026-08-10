using Audiola.Models;

namespace Audiola.Services;

/// <summary>
/// Vorbelegung und Rückkanäle des Export-Dialogs (Format, Bitrate, Tags, Cover, Lyrics).
/// </summary>
/// <param name="DefaultFileName">Vorgeschlagener Dateiname.</param>
/// <param name="Seed">Vorbelegung der Tag-Felder (z. B. die projektweiten Song-Metadaten).</param>
/// <param name="SeedLyrics">Optionaler Liedtext, falls die Tags keinen haben.</param>
/// <param name="GenerateLyrics">Optionaler Callback zum Erzeugen von Lyrics im Dialog (Whisper).</param>
/// <param name="ElevenLabsAvailable">Schaltet die Cloud-Transkription im Dialog frei.</param>
/// <param name="PreviewAsync">Rendert die Auswahl in eine Temp-Datei und zeigt die Vorschau.</param>
public sealed record ExportDialogRequest(
    string DefaultFileName,
    AudioMetadata Seed,
    string? SeedLyrics,
    Func<bool, Task<string?>>? GenerateLyrics,
    bool ElevenLabsAvailable,
    Func<ExportRequest, Task> PreviewAsync);

/// <summary>Antwort der Speichern-Rückfrage beim Schließen/Wechseln eines Projekts.</summary>
public enum SaveDiscardCancel
{
    Save,
    Discard,
    Cancel
}

/// <summary>
/// Die eigenen Fenster, die geteilter Code selbst öffnet. Jeder Host stellt seine
/// Implementierung; vorher instanzierten ViewModels und Dienste die WPF-Fenster direkt.
/// </summary>
public interface IAppDialogs
{
    /// <summary>Dreifach-Rückfrage „Speichern / Verwerfen / Abbrechen".</summary>
    Task<SaveDiscardCancel> AskSaveDiscardCancelAsync(string title, string message);

    /// <summary>Modaler Einrichtungs-Assistent (erster Start und Hilfe-Menü).</summary>
    void ShowSetupWizard();

    /// <summary>Modaler „Spur mastern"-Dialog (EQ → Kompressor → LUFS) für eine Spur.</summary>
    void ShowTrackMastering(object trackViewModel);

    /// <summary>Nicht-modales Einsing-Studio (Karaoke: Backing + Mikro, Ton-Feedback).</summary>
    void OpenSingAlong();

    /// <summary>Ja/Nein-Rückfrage (vorher <c>MessageBox.Show(..., YesNo)</c>).</summary>
    bool Confirm(string title, string message);

    /// <summary>Export-Dialog; <c>null</c> = abgebrochen.</summary>
    Task<ExportRequest?> ShowExportAsync(ExportDialogRequest request);

    /// <summary>Modale Datei-Vorschau (eingebetteter Browser bzw. Systembrowser als Rückfall).</summary>
    Task ShowFilePreviewAsync(string url, string fileName);
}
