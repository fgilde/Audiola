using Audiola.Services;
using Avalonia.Controls;

namespace Audiola.Avalonia.Platform;

/// <summary>
/// Shell-Navigation der Avalonia-Fassung: hält die Seiten als Singletons (wie die WPF-Frame-
/// Navigation) und tauscht den Inhalt des Arbeitsbereichs. Seiten, die
/// <see cref="INavigationAware"/> umsetzen, werden beim Wechsel benachrichtigt.
/// </summary>
public sealed class AvaloniaShellNavigation(IServiceProvider services) : IShellNavigation
{
    private ContentControl? _host;

    public event EventHandler<ShellPage>? Navigated;

    /// <summary>Vom Hauptfenster einmalig gesetzt.</summary>
    public void SetHost(ContentControl host) => _host = host;

    public void Navigate(ShellPage page)
    {
        if (_host is null) return;
        if (!Views.ShellPages.ViewTypes.TryGetValue(page, out var viewType)) return;
        if (services.GetService(viewType) is not Control view) return;
        if (ReferenceEquals(_host.Content, view)) return;   // schon aktiv

        (_host.Content as INavigationAware)?.OnNavigatedFrom();
        _host.Content = view;
        (view as INavigationAware)?.OnNavigatedTo();
        Navigated?.Invoke(this, page);
    }
}

/// <summary>
/// Wird von Seiten umgesetzt, die beim Anzeigen/Verlassen aufräumen oder nachladen müssen
/// (Gegenstück zum gleichnamigen WPF-UI-Vertrag).
/// </summary>
public interface INavigationAware
{
    void OnNavigatedTo();

    void OnNavigatedFrom();
}
