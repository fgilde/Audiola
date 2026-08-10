using Avalonia;
using System.IO;
using System.Runtime.InteropServices;
using Velopack;
using Velopack.Sources;

namespace Audiola.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        App.StartupFile = args.FirstOrDefault(File.Exists);
        if (UpdateChannel is { } channel)
            StartBackgroundUpdates(channel);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void StartBackgroundUpdates(string channel) => _ = Task.Run(async () =>
    {
        try
        {
            var manager = new UpdateManager(new GithubSource("https://github.com/fgilde/Audiola", null, false),
                new UpdateOptions { ExplicitChannel = channel });
            if (!manager.IsInstalled) return;
            var update = await manager.CheckForUpdatesAsync();
            if (update is not null)
            {
                await manager.DownloadUpdatesAsync(update);
                manager.WaitExitThenApplyUpdates(update);
            }
        }
        catch { }
    });

    private static string? UpdateChannel =>
        OperatingSystem.IsWindows() ? "audiola-win-x64" :
        OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "audiola-osx-arm64" :
        OperatingSystem.IsMacOS() ? "audiola-osx-x64" :
        OperatingSystem.IsLinux() ? "audiola-linux-x64" :
        null;

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
