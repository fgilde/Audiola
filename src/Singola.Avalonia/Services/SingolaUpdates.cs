using System.Runtime.InteropServices;
using Velopack;
using Velopack.Sources;

namespace Singola.Services;

/// <summary>
/// Sucht im Hintergrund nach Updates und lädt sie herunter — sagt es der Oberfläche aber auch.
/// Vorher wurde still heruntergeladen und beim Beenden angewendet; niemand erfuhr davon.
/// Jeder Plattform-Build hat seinen eigenen Velopack-Kanal, deshalb ist ExplicitChannel Pflicht.
/// </summary>
public sealed class SingolaUpdates
{
    private readonly UpdateManager? _manager;
    private UpdateInfo? _ready;

    public SingolaUpdates()
    {
        try
        {
            _manager = new UpdateManager(
                new GithubSource("https://github.com/fgilde/Audiola", null, prerelease: false),
                new UpdateOptions { ExplicitChannel = Channel });
        }
        catch
        {
            _manager = null;
        }
    }

    public static string Channel =>
        OperatingSystem.IsWindows() ? "singola-win-x64" :
        OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "singola-osx-arm64" :
        OperatingSystem.IsMacOS() ? "singola-osx-x64" :
        "singola-linux-x64";

    /// <summary>Meldet die Version, sobald ein Update heruntergeladen und startklar ist.</summary>
    public event Action<string>? UpdateReady;

    /// <summary>Läuft nur in einer Velopack-Installation; im Dev-Build ein No-Op.</summary>
    public void StartCheck() => _ = Task.Run(async () =>
    {
        try
        {
            if (_manager is null || !_manager.IsInstalled) return;
            var update = await _manager.CheckForUpdatesAsync();
            if (update is null) return;
            await _manager.DownloadUpdatesAsync(update);
            _ready = update;
            UpdateReady?.Invoke(update.TargetFullRelease.Version.ToString());
        }
        catch { /* Updates sind Beiwerk — eine Party darf daran nicht scheitern. */ }
    });

    /// <summary>Wendet das geladene Update an und startet Singola neu.</summary>
    public void ApplyAndRestart()
    {
        if (_manager is null || _ready is null) return;
        try { _manager.ApplyUpdatesAndRestart(_ready); }
        catch { /* Beim nächsten regulären Start greift Velopack ohnehin. */ }
    }
}
