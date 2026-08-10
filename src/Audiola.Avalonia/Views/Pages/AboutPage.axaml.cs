using System.Diagnostics;
using System.Reflection;
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

    /// <summary>Versionsanzeige aus der Assembly (Fallback 0.1.0).</summary>
    public string VersionText
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            var s = v is null ? "0.1.0" : $"{v.Major}.{v.Minor}.{v.Build}";
            return $"Version {s}";
        }
    }

    public string CopyrightText => $"© {DateTime.Now.Year} Florian Gilde";

    /// <summary>Nennt die tatsächlich laufende Plattform (die App läuft auf Windows, macOS und Linux).</summary>
    public string TechText =>
        $".NET 10 · Avalonia (Fluent) · NAudio · CommunityToolkit.Mvvm · TagLibSharp · Velopack. " +
        $"Läuft auf {RuntimeInformation.OSDescription.Trim()} ({RuntimeInformation.ProcessArchitecture}). " +
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
