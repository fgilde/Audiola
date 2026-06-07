using System.IO;
using Velopack;

namespace Audiola;

/// <summary>
/// Expliziter Einstiegspunkt, damit Velopack ZUERST läuft. Seine Install/Uninstall/Update-Hooks
/// werden beim Start über Kommandozeilen-Argumente ausgelöst; startet WPF vorher, feuern sie nie.
/// Registriert über <c>&lt;StartupObject&gt;</c> in der csproj.
/// </summary>
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            VelopackApp.Build().Run();
        }
        catch (Exception ex)
        {
            // Ein Velopack-Fehler darf den normalen App-Start nie blockieren.
            try
            {
                File.AppendAllText(
                    Path.Combine(AppContext.BaseDirectory, "audiola.log"),
                    $"[{DateTimeOffset.UtcNow:O}] [Velopack] {ex}\n\n");
            }
            catch { /* ignore */ }
        }

        var app = new App();
        app.InitializeComponent();
        return app.Run();
    }
}
