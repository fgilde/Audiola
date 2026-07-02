using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Audiola.Services;

/// <summary>
/// Einheitliche Snackbar-Meldungen (Erfolg/Hinweis/Warnung/Fehler) — ersetzt die ~28 duplizierten
/// <c>Show(..., ControlAppearance, new SymbolIcon(...), TimeSpan)</c>-Aufrufe durch eine Zeile.
/// </summary>
public static class SnackbarExtensions
{
    public static void Success(this ISnackbarService snackbar, string title, string message, int seconds = 3)
        => snackbar.Show(title, message, ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(seconds));

    public static void Info(this ISnackbarService snackbar, string title, string message, int seconds = 3)
        => snackbar.Show(title, message, ControlAppearance.Info,
            new SymbolIcon(SymbolRegular.Info24), TimeSpan.FromSeconds(seconds));

    public static void Warning(this ISnackbarService snackbar, string title, string message, int seconds = 3)
        => snackbar.Show(title, message, ControlAppearance.Caution,
            new SymbolIcon(SymbolRegular.Warning24), TimeSpan.FromSeconds(seconds));

    public static void Error(this ISnackbarService snackbar, string title, string message, int seconds = 4)
        => snackbar.Show(title, message, ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(seconds));
}
