using Avalonia;
using Velopack;

namespace Singola.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        // Die Update-Suche läuft jetzt über SingolaUpdates, damit die App das Ergebnis auch anzeigt.
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
