using System.IO;
using Velopack;
using Velopack.Sources;

namespace Audiola.Services;

/// <summary>
/// Auto-Update über die GitHub-Releases via Velopack. Funktioniert nur, wenn die App über das
/// Velopack-Setup installiert wurde (<see cref="IsManaged"/>); im Dev-/Portable-Build ein No-Op.
/// </summary>
public sealed class UpdateService
{
    public const string Repository = "https://github.com/fgilde/Audiola";

    private readonly UpdateManager? _mgr;

    public UpdateService()
    {
        try
        {
            _mgr = new UpdateManager(new GithubSource(Repository, null, prerelease: false));
        }
        catch (Exception ex)
        {
            Log("ctor", ex);
        }
    }

    /// <summary>True, wenn die App aus einer Velopack-Installation läuft (nur dann Self-Update).</summary>
    public bool IsManaged => _mgr?.IsInstalled == true;

    public async Task<UpdateInfo?> CheckAsync()
    {
        if (_mgr is null || !IsManaged) return null;
        try { return await _mgr.CheckForUpdatesAsync(); }
        catch (Exception ex) { Log("CheckAsync", ex); return null; }
    }

    public async Task<bool> DownloadAsync(UpdateInfo info, IProgress<int>? progress = null)
    {
        if (_mgr is null) return false;
        try
        {
            await _mgr.DownloadUpdatesAsync(info, progress is null ? null : p => progress.Report(p));
            return true;
        }
        catch (Exception ex) { Log("DownloadAsync", ex); return false; }
    }

    public void ApplyAndRestart(UpdateInfo info)
    {
        if (_mgr is null) return;
        try { _mgr.ApplyUpdatesAndRestart(info); }
        catch (Exception ex) { Log("ApplyAndRestart", ex); }
    }

    private static void Log(string where, Exception ex)
    {
        try
        {
            File.AppendAllText(
                Path.Combine(AppContext.BaseDirectory, "audiola.log"),
                $"[{DateTimeOffset.UtcNow:O}] [UpdateService.{where}] {ex}\n\n");
        }
        catch { /* ignore */ }
    }
}
