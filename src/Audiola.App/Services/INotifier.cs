namespace Audiola.Services;

/// <summary>
/// Einheitliche, flüchtige Meldungen (Erfolg/Hinweis/Warnung/Fehler) — der Host stellt
/// das Toast-/Snackbar-Control. Ersetzt die frühere direkte WPF-UI-Kopplung der ViewModels.
/// </summary>
public interface INotifier
{
    void Success(string title, string message, int seconds = 3);
    void Info(string title, string message, int seconds = 3);
    void Warning(string title, string message, int seconds = 3);
    void Error(string title, string message, int seconds = 4);
}
