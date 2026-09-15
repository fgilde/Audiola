using Avalonia;
using System.IO;
using Velopack;

namespace Audiola.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        App.StartupFile = args.FirstOrDefault(File.Exists);
        // Entwicklungs-Hilfe: "--page Voices" öffnet direkt eine Seite (für Screenshots und Tests).
        var pageIndex = Array.IndexOf(args, "--page");
        if (pageIndex >= 0 && pageIndex + 1 < args.Length) App.StartupPage = args[pageIndex + 1];
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Updates laufen über Audiola.Services.UpdateService (geteilt mit der WPF-Fassung und
    // mit Rückfrage). Ein zweiter UpdateManager hier würde sich mit dem um die
    // Paketsperre streiten.

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
