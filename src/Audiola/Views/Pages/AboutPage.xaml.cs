using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Audiola.Views.Pages;

public partial class AboutPage : Page
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

    private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch { /* kein Standardbrowser verfügbar — ignorieren */ }
        e.Handled = true;
    }
}
