using System.Diagnostics;
using Audiola.Services;
using System.Runtime.InteropServices;
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
}
