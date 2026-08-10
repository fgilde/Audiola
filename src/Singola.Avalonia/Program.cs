using Avalonia;
using System.Runtime.InteropServices;
using Velopack;
using Velopack.Sources;

namespace Singola.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        StartBackgroundUpdates(UpdateChannel);
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

    private static string UpdateChannel =>
        OperatingSystem.IsWindows() ? "singola-win-x64" :
        OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "singola-osx-arm64" :
        OperatingSystem.IsMacOS() ? "singola-osx-x64" :
        OperatingSystem.IsLinux() ? "singola-linux-x64" :
        throw new PlatformNotSupportedException("Singola updates are not configured for this platform.");

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
