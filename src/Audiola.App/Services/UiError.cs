using System.IO;

namespace Audiola.Services;

/// <summary>
/// Zeigt Fehler in einer kopierbaren MessageBox (Strg+C kopiert den ganzen Text) —
/// im Gegensatz zum flüchtigen Snackbar-Toast, den man nicht markieren kann.
/// Der Host verdrahtet seine Fenster-Implementierung über <see cref="Configure"/>.
///
/// Jeder Fehler wandert zusätzlich in <c>audiola.log</c> neben der Anwendung: ein
/// weggeklickter Hinweis war bisher spurlos, was die Ursachensuche unnötig schwer macht.
/// </summary>
public static class UiError
{
    private static Action<string, string>? _show;

    /// <summary>Vom Host einmalig beim Start gesetzt.</summary>
    public static void Configure(Action<string, string> show) => _show = show;

    public static void Show(string title, string message)
    {
        Log(title, message);
        DispatcherHelper.OnUi(() => _show?.Invoke(title, message));
    }

    /// <summary>Pfad des Protokolls — für den Hinweis „Details stehen in …“.</summary>
    public static string LogPath => Path.Combine(AppContext.BaseDirectory, "audiola.log");

    private static void Log(string title, string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTimeOffset.Now:O}] [{title}] {message}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { /* Ein klemmendes Protokoll darf den Hinweis nicht verschlucken. */ }
    }
}
