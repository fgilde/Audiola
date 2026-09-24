using System.Diagnostics;
using Audiola.Services;
using System.Runtime.InteropServices;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Audiola.Avalonia.Views.Pages;

public partial class AboutPage : UserControl
{
    public AboutPage()
    {
        DataContext = this;
        InitializeComponent();
    }

    /// <summary>Version wie im Fenstertitel — eine gemeinsame Quelle (siehe AppVersion).</summary>
    public string VersionText => AppVersion.Display;

    public string CopyrightText => $"© {DateTime.Now.Year} gilde.org";

    /// <summary>Laufende Plattform als kurzer Zusatz neben der Version.</summary>
    public string PlatformText =>
        $"{(OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsMacOS() ? "macOS" : "Linux")} · {RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}";

    /// <summary>Nennt die tatsächlich laufende Plattform (die App läuft auf Windows, macOS und Linux).</summary>
    public string TechText =>
        $".NET 10 · Avalonia (Fluent) · NAudio · CommunityToolkit.Mvvm · TagLibSharp · Velopack. " +
        $"Läuft auf {RuntimeInformation.OSDescription.Trim()}. " +
        "Lokale KI-Komponenten laufen über eine verwaltete Python-Umgebung (Demucs, faster-whisper, seed-vc u. a.).";

    private void OnOpenLink(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { Tag: string url }) return;
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { /* kein Standardbrowser verfügbar — ignorieren */ }
    }

    /// <summary>Öffnet die Connect-Seite (Kontakt bzw. Unterstützen) im Standardbrowser und
    /// gibt Theme, Akzentfarbe und Sprache der App mit — die Widgets von gilde.org laufen im
    /// Web, ein eingebetteter Browser wäre auf allen drei Plattformen eine eigene Baustelle.</summary>
    private void OnOpenConnect(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { Tag: string kind }) return;
        var theme = Application.Current?.ActualThemeVariant == ThemeVariant.Light ? "light" : "dark";
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var url = $"https://audiola.de/connect.html?w={kind}&theme={theme}&lang={lang}" +
                  $"&accent={Uri.EscapeDataString(AccentHex())}";
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { /* kein Standardbrowser verfügbar — ignorieren */ }
    }

    /// <summary>Aktuelle Akzentfarbe der Oberfläche als #RRGGBB.</summary>
    private static string AccentHex()
    {
        var app = Application.Current;
        if (app is not null && app.TryGetResource("DawAccent", app.ActualThemeVariant, out var value)
            && value is Color c)
        {
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
        return "#3F8CFF";
    }
}

