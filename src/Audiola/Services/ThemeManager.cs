using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Audiola.Services;

/// <summary>
/// Wendet das App-Theme (Light/Dark) an — auch LIVE, ohne Neustart. Zwei Mechanismen:
///
/// 1. <see cref="ApplicationThemeManager"/> tauscht das WPF-UI-Theme-Dictionary (Dark/Light.xaml)
///    → alle DynamicResource-Verweise auf WPF-UI-Brushes lösen frisch auf.
/// 2. <see cref="ApplyPalette"/> ERSETZT unsere Brushes (Daw* + überschriebene WPF-UI-System-Keys,
///    inkl. der Elevation-Border) durch frische, gefrorene Objekte im BASE-Dictionary der App.
///    Ersetzen statt Mutieren, weil WPF Setter-Werte beim Style-Sealing einfriert; deshalb müssen
///    per Konvention ALLE Verweise auf diese Keys DynamicResource sein (nie StaticResource).
///
/// Bewusst NICHT: WPF-UI-Dictionaries zur Laufzeit neu laden oder isoliert parsen — isoliert
/// geparst lösen deren interne StaticResources (z. B. SystemAccentColorPrimary) nicht auf
/// (XamlParseException), und ein Reload re-applied den FluentWindow-Style (AllowsTransparency
/// nach Show ⇒ Exception).
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
        // Elevation-Border der WPF-UI-Controls (Button/TextBox …): im Original ein Gradient aus dem
        // Theme-Dict; als flacher Stroke überschrieben, damit er unsere Ersetz-Maschinerie nutzt.
        ("ControlElevationBorderBrush", "DawStrokeStrong"),
        ("TextControlElevationBorderBrush", "DawStrokeStrong"),
        ("CircleElevationBorderBrush", "DawStrokeStrong"),
    ];

    public static void Apply(string? theme)
    {
        IsLight = string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase);
        var appTheme = IsLight ? ApplicationTheme.Light : ApplicationTheme.Dark;

        // WPF-UI-eigene Controls (Popups, ScrollBars, Slider-Innereien, Fokus …).
        ApplicationThemeManager.Apply(appTheme, updateAccent: false);

        ApplyPalette(IsLight);
        UpdateWindowChrome(appTheme);

        var accent = IsLight ? Color.FromRgb(0x2F, 0x6F, 0xE0) : Color.FromRgb(0x5B, 0x8C, 0xFF);
        ApplicationAccentColorManager.Apply(accent, appTheme, systemGlassColor: false);
    }

    /// <summary>Ersetzt jeden gemappten Brush durch einen frischen (gefrorenen) mit der Farbe der
    /// gewünschten Palette — im BASE-Dictionary der App-Ressourcen, das jeden Merged-Eintrag
    /// überschattet. Alle Verweise auf Daw-/System-Brushes laufen per DynamicResource (per
    /// Konvention, siehe Sweep) und lösen den Ersatz sofort auf. Mutation der Brush-Objekte wäre
    /// keine Option: WPF friert Setter-Werte beim Style-Sealing ein (Brush.Color danach fix).</summary>
    private static void ApplyPalette(bool light)
    {
        var palette = new ResourceDictionary { Source = ThemeUri($"DawColors.{(light ? "Light" : "Dark")}.xaml") };
        var res = Application.Current.Resources;
        foreach (var (brushKey, colorKey) in Map)
        {
            if (palette[colorKey] is not Color c) continue;
            var b = new SolidColorBrush(c);
            b.Freeze();             // unveränderlich + Cross-Thread-sicher; Wechsel ersetzt das Objekt
            res[brushKey] = b;
        }
    }

    /// <summary>Setzt nur das native Dark-Mode-Flag der Titelleisten (DWM) — bewusst KEIN
    /// WindowBackgroundManager: der manipuliert den Fenster-Background und kann das Fenster
    /// „leeren". Der Inhalt färbt komplett über die DynamicResource-Brushes.</summary>
    private static void UpdateWindowChrome(ApplicationTheme theme)
    {
        foreach (Window w in Application.Current.Windows)
        {
            try
            {
                if (theme == ApplicationTheme.Dark)
                    Wpf.Ui.Interop.UnsafeNativeMethods.ApplyWindowDarkMode(w);
                else
                    Wpf.Ui.Interop.UnsafeNativeMethods.RemoveWindowDarkMode(w);
            }
            catch { /* rein kosmetisch — nie fatal */ }
        }
    }

    private static Uri ThemeUri(string file) =>
        new($"pack://application:,,,/Themes/{file}", UriKind.Absolute);
}
