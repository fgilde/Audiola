using System.Windows;

namespace Audiola.Services;

/// <summary>
/// Zeigt Fehler in einer kopierbaren MessageBox (Strg+C kopiert den ganzen Text) —
/// im Gegensatz zum flüchtigen Snackbar-Toast, den man nicht markieren kann.
/// </summary>
public static class UiError
{
    public static void Show(string title, string message)
    {
        void Display() => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
            dispatcher.Invoke(Display);
        else
            Display();
    }
}
