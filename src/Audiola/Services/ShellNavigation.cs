using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Audiola.Services;

/// <summary>
/// Schlanke Shell-Navigation der DAW-Oberfläche: hostet die (Singleton-)Seiten in einem
/// <see cref="Frame"/> im Zentrum des Hauptfensters. Ersetzt die frühere NavigationView
/// (Settings-App-Muster) — die Werkzeugleiste (Rail) und Menüs rufen <see cref="Navigate"/> auf.
/// </summary>
public interface IShellNavigation
{
    /// <summary>Zeigt die Seite des Typs im Haupt-Frame (löst sie aus dem DI-Container).</summary>
    void Navigate(Type pageType);

    /// <summary>Wird nach jedem Seitenwechsel ausgelöst (für die aktive Markierung in der Rail).</summary>
    event EventHandler<Type>? Navigated;
}

public sealed class ShellNavigation(IServiceProvider services) : IShellNavigation
{
    private Frame? _frame;

    public event EventHandler<Type>? Navigated;

    /// <summary>Vom Hauptfenster einmalig gesetzt; hält das Journal leer (kein Back-Stack nötig).</summary>
    public void SetFrame(Frame frame)
    {
        _frame = frame;
        frame.Navigated += (_, _) =>
        {
            while (frame.CanGoBack) frame.RemoveBackEntry();
        };
    }

    public void Navigate(Type pageType)
    {
        if (_frame is null) return;
        if (services.GetService(pageType) is not Page page) return;
        if (ReferenceEquals(_frame.Content, page)) return;   // schon aktiv

        (_frame.Content as INavigationAware)?.OnNavigatedFrom();
        _frame.Navigate(page);
        (page as INavigationAware)?.OnNavigatedTo();
        Navigated?.Invoke(this, pageType);
    }
}
