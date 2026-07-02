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

    private static bool _appliedOnce;

    public static void Apply(string? theme)
    {
        IsLight = string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase);

        // WPF-UI-eigene Controls (Popups, ScrollBars, Slider-Innereien, Fokus …).
        ApplicationThemeManager.Apply(
            IsLight ? ApplicationTheme.Light : ApplicationTheme.Dark, updateAccent: false);

        // Beim LIVE-Wechsel zusätzlich das WPF-UI-Controls-Dictionary neu laden: dessen Brushes
        // (Button-Border, Hover, ComboBox …) hängen per DynamicResource an den COLOR-Keys des
        // getauschten Theme-Dictionaries — und Freezable-Unterproperties (Brush.Color) werden
        // beim Dictionary-Tausch nicht neu ausgewertet. Frisch instanziiert lösen sie korrekt auf.
        if (_appliedOnce) RefreshWpfUiControlsDictionary();
        _appliedOnce = true;

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

    /// <summary>Ersetzt Wpf.Ui.xaml an Ort und Stelle durch eine frische Instanz (Reihenfolge bleibt).
    /// Nötig, weil WPF-UI-Control-Styles Theme-Brushes teils per StaticResource einbetten — die
    /// zeigen nach dem Theme-Tausch sonst dauerhaft auf die alten (verwaisten) Brush-Objekte.</summary>
    private static void RefreshWpfUiControlsDictionary()
    {
        // Offene Fenster zuerst vom Ressourcen-Style abkoppeln (Expression → lokaler Wert):
        // Das Neuladen würde sonst den FluentWindow-Style neu anwenden, dessen Setter
        // AllowsTransparency schreibt — nach dem Anzeigen des Fensters eine Exception.
        foreach (Window w in Application.Current.Windows)
            if (w.Style is { } s) w.Style = s;

        var dicts = Application.Current.Resources.MergedDictionaries;
        for (var i = 0; i < dicts.Count; i++)
        {
            if (dicts[i].Source is { } src && src.OriginalString.Contains("Wpf.Ui.xaml", StringComparison.OrdinalIgnoreCase))
            {
                dicts[i] = new ResourceDictionary { Source = src };
                return;
            }
        }
    }

    private static Uri ThemeUri(string file) =>
        new($"pack://application:,,,/Themes/{file}", UriKind.Absolute);
}
