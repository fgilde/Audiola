using Velopack;
using Velopack.Sources;

namespace Singola;

/// <summary>Einstiegspunkt: Velopack-Hooks (Install/Update) laufen VOR dem WPF-Start.</summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();

        // Leises Auto-Update im Hintergrund (nur in der installierten Version relevant).
        _ = Task.Run(async () =>
        {
            try
            {
                var mgr = new UpdateManager(
                    new GithubSource("https://github.com/fgilde/Audiola", null, false),
                    new UpdateOptions { ExplicitChannel = "singola-win" });
                if (!mgr.IsInstalled) return;
                var info = await mgr.CheckForUpdatesAsync();
                if (info is null) return;
                await mgr.DownloadUpdatesAsync(info);
                mgr.WaitExitThenApplyUpdates(info);   // greift beim nächsten Start
            }
            catch { /* Updates sind optional — nie den Start stören */ }
        });

        app.Run();
    }
}
