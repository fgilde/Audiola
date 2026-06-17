using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Wpf.Ui.Controls;

namespace Audiola.Views.Dialogs;

/// <summary>
/// Zeigt eine Datei-Vorschau (z. B. die mudex-Preview-Seite) in einer eingebetteten WebView2.
/// Fällt auf den Standardbrowser zurück, wenn die WebView2-Runtime fehlt.
/// </summary>
public partial class PreviewDialog : FluentWindow
{
    private readonly string _url;

    public PreviewDialog(string url, string? title = null)
    {
        _url = url;
        InitializeComponent();
        if (!string.IsNullOrWhiteSpace(title)) TitleBarControl.Title = $"Vorschau — {title}";
        Loaded += async (_, _) => await InitAsync();
        Closed += (_, _) => { try { Web.Dispose(); } catch { /* egal */ } };
    }

    private async Task InitAsync()
    {
        try
        {
            // Eigener, isolierter Benutzerdatenordner (kein Schreiben ins Programmverzeichnis).
            var dataDir = Path.Combine(Path.GetTempPath(), "Audiola", "webview2");
            Directory.CreateDirectory(dataDir);
            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: dataDir);
            await Web.EnsureCoreWebView2Async(env);

            Web.CoreWebView2.Navigate(_url);
            Web.NavigationCompleted += (_, _) =>
            {
                LoadingRing.Visibility = Visibility.Collapsed;
                Web.Visibility = Visibility.Visible;
            };
        }
        catch (Exception ex)
        {
            // WebView2-Runtime nicht installiert o. Ä. → im Standardbrowser öffnen.
            try { Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true }); } catch { /* egal */ }
            Audiola.Services.UiError.Show("Vorschau im Fenster nicht möglich",
                "Die Datei wird im Standardbrowser geöffnet (WebView2-Runtime nicht verfügbar).\n\n" + ex.Message);
            Close();
        }
    }
}
