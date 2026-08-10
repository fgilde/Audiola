namespace Audiola.Services;

/// <summary>Die Seiten der DAW-Shell. Host-neutraler Schlüssel statt <c>typeof(SomePage)</c>.</summary>
public enum ShellPage
{
    Home,
    Editor,
    Timeline,
    Equalizer,
    Mastering,
    SpatialAudio,
    Voices,
    Variation,
    Provenance,
    Evaluation,
    Settings,
    Metadata,
    About
}

/// <summary>
/// Schlanke Shell-Navigation der DAW-Oberfläche: der Host hostet die (Singleton-)Seiten im
/// Zentrum des Hauptfensters. Die Werkzeugleiste (Rail) und Menüs rufen <see cref="Navigate"/> auf.
/// </summary>
public interface IShellNavigation
{
    /// <summary>Zeigt die Seite im Haupt-Bereich.</summary>
    void Navigate(ShellPage page);

    /// <summary>Wird nach jedem Seitenwechsel ausgelöst (für die aktive Markierung in der Rail).</summary>
    event EventHandler<ShellPage>? Navigated;
}
