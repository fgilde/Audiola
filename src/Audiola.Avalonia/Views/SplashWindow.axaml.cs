using Avalonia.Controls;

namespace Audiola.Avalonia.Views;

/// <summary>
/// Startbild mit Logo und Markenverlauf. Wird vor dem Aufbau des Containers gezeigt und
/// beim Erscheinen des Hauptfensters geschlossen; der Balken zeigt den Fortschritt der
/// Startschritte.
/// </summary>
public partial class SplashWindow : Window
{
    private const double BarWidth = 460;

    public SplashWindow() => InitializeComponent();

    /// <summary>Fortschritt 0..1 — der Balken wandert animiert mit.</summary>
    public void SetProgress(double ratio) => LoadBar.Width = Math.Clamp(ratio, 0, 1) * BarWidth;
}
