using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace Audiola.Services;

/// <summary>
/// Wendet das App-Theme (Light/Dark) an. Die Studio-Palette (DawColors.*.xaml) liefert nur die
/// FARBWERTE; die Brushes (Daw*Brush + WPF-UI-System-Overrides) leben in DawTheme.xaml und bleiben
/// als OBJEKTE dauerhaft bestehen. Beim Wechsel wird nur ihre <see cref="SolidColorBrush.Color"/>
/// gesetzt.
///
/// Warum nicht einfach das Farb-Dictionary austauschen? Weil eine DynamicResource auf einer
/// Freezable-Unterproperty (Brush.Color) NICHT zuverlässig neu ausgewertet wird, wenn das
/// referenzierte Dictionary zur Laufzeit ersetzt wird — der Wechsel griffe dann erst nach Neustart.
/// Das direkte Setzen der Color am stabilen Brush-Objekt wirkt dagegen sofort und hält zugleich
/// alle StaticResource-Referenzen in den Views gültig.
/// </summary>
public static class ThemeManager
{
    public static bool IsLight { get; private set; }

    /// <summary>
    /// Zuordnung Brush-Ressource → Farb-Key aus DawColors.*.xaml. Enthält die eigenen Daw*Brushes
    /// UND die überschriebenen WPF-UI-System-Keys, damit beide Referenzarten (Static/Dynamic) beim
    /// Wechsel sofort umfärben. Neue Brushes in DawTheme.xaml hier ergänzen.
    /// </summary>
    private static readonly (string Brush, string Color)[] Map =
    [
        ("DawBgBrush", "DawBg"), ("DawPanelBrush", "DawPanel"), ("DawCardBrush", "DawCard"),
        ("DawHoverBrush", "DawHover"), ("DawStrokeBrush", "DawStroke"), ("DawStrokeStrongBrush", "DawStrokeStrong"),
        ("DawTextBrush", "DawText"), ("DawTextDimBrush", "DawTextDim"), ("DawTextFaintBrush", "DawTextFaint"),
        ("DawAccentBrush", "DawAccent"), ("DawAccentLightBrush", "DawAccentLight"), ("DawAccentSoftBrush", "DawAccentSoft"),
        ("DawGreenBrush", "DawGreen"), ("DawYellowBrush", "DawYellow"), ("DawRedBrush", "DawRed"), ("DawRecBrush", "DawRec"),
        ("DawTimelineBrush", "DawTimeline"), ("DawScrollThumbBrush", "DawScrollThumb"), ("DawScrollTrackBrush", "DawScrollTrack"),
        // WPF-UI-System-Keys, die viele Views per DynamicResource nutzen:
        ("ApplicationBackgroundBrush", "DawBg"), ("SolidBackgroundFillColorBaseBrush", "DawBg"),
        ("SolidBackgroundFillColorSecondaryBrush", "DawPanel"), ("CardBackgroundFillColorDefaultBrush", "DawPanel"),
        ("CardBackgroundFillColorSecondaryBrush", "DawCard"), ("ControlFillColorDefaultBrush", "DawCard"),
        ("ControlFillColorSecondaryBrush", "DawHover"), ("CardStrokeColorDefaultBrush", "DawStroke"),
        ("ControlStrokeColorDefaultBrush", "DawStrokeStrong"), ("TextFillColorPrimaryBrush", "DawText"),
        ("TextFillColorSecondaryBrush", "DawTextDim"), ("TextFillColorTertiaryBrush", "DawTextFaint"),
    ];

    public static void Apply(string? theme)
    {
        IsLight = string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase);

        // WPF-UI-eigene Controls (Popups, ScrollBars, Slider-Innereien, Fokus …).
        ApplicationThemeManager.Apply(
            IsLight ? ApplicationTheme.Light : ApplicationTheme.Dark, updateAccent: false);

        ApplyPalette(IsLight);

        var accent = IsLight ? Color.FromRgb(0x2F, 0x6F, 0xE0) : Color.FromRgb(0x5B, 0x8C, 0xFF);
        ApplicationAccentColorManager.Apply(accent,
            IsLight ? ApplicationTheme.Light : ApplicationTheme.Dark, systemGlassColor: false);
    }

    /// <summary>Setzt die Farbe jedes gemappten Brushes auf den Wert der gewünschten Palette.</summary>
    private static void ApplyPalette(bool light)
    {
        var palette = new ResourceDictionary { Source = ThemeUri($"DawColors.{(light ? "Light" : "Dark")}.xaml") };
        var res = Application.Current.Resources;
        foreach (var (brushKey, colorKey) in Map)
        {
            if (palette[colorKey] is not Color c) continue;
            if (res[brushKey] is SolidColorBrush b && !b.IsFrozen)
                b.Color = c;                        // Objekt bleibt stabil → Static/Dynamic greifen sofort
            else
                res[brushKey] = new SolidColorBrush(c);  // Fallback (fehlt/frozen)
        }
    }

    private static Uri ThemeUri(string file) =>
        new($"pack://application:,,,/Themes/{file}", UriKind.Absolute);
}
