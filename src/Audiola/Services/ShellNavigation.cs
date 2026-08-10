using System.Windows.Controls;
using Audiola.Views.Pages;
using Wpf.Ui.Controls;

namespace Audiola.Services;

/// <summary>
/// WPF-Umsetzung der Shell-Navigation: hostet die (Singleton-)Seiten in einem
/// <see cref="Frame"/> im Zentrum des Hauptfensters. Ersetzt die frühere NavigationView
/// (Settings-App-Muster) — die Werkzeugleiste (Rail) und Menüs rufen <see cref="Navigate"/> auf.
/// </summary>
public sealed class ShellNavigation(IServiceProvider services) : IShellNavigation
{
    /// <summary>Zuordnung des host-neutralen Schlüssels auf den WPF-Seitentyp.</summary>
    private static readonly Dictionary<ShellPage, Type> PageTypes = new()
    {
        [ShellPage.Home] = typeof(HomePage),
        [ShellPage.Editor] = typeof(EditorPage),
        [ShellPage.Timeline] = typeof(TimelinePage),
        [ShellPage.Equalizer] = typeof(EqualizerPage),
        [ShellPage.Mastering] = typeof(MasteringPage),
        [ShellPage.SpatialAudio] = typeof(SpatialAudioPage),
        [ShellPage.Voices] = typeof(VoicesPage),
        [ShellPage.Variation] = typeof(VariationPage),
        [ShellPage.Provenance] = typeof(ProvenancePage),
        [ShellPage.Evaluation] = typeof(EvaluationPage),
        [ShellPage.Settings] = typeof(SettingsPage),
        [ShellPage.Metadata] = typeof(MetadataPage),
        [ShellPage.About] = typeof(AboutPage)
    };

    private Frame? _frame;

    public event EventHandler<ShellPage>? Navigated;

    /// <summary>Vom Hauptfenster einmalig gesetzt; hält das Journal leer (kein Back-Stack nötig).</summary>
    public void SetFrame(Frame frame)
    {
        _frame = frame;
        frame.Navigated += (_, _) =>
        {
            while (frame.CanGoBack) frame.RemoveBackEntry();
        };
    }

    public void Navigate(ShellPage page)
    {
        if (_frame is null) return;
        if (!PageTypes.TryGetValue(page, out var pageType)) return;
        if (services.GetService(pageType) is not Page target) return;
        if (ReferenceEquals(_frame.Content, target)) return;   // schon aktiv

        (_frame.Content as INavigationAware)?.OnNavigatedFrom();
        _frame.Navigate(target);
        (target as INavigationAware)?.OnNavigatedTo();
        Navigated?.Invoke(this, page);
    }
}
