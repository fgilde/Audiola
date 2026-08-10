using System.IO;
using Audiola.Models;
using Audiola.ViewModels;

namespace Audiola.Services;

public sealed class TrackLoaderService : ITrackLoader
{
    private const int MaxRecent = 8;

    private readonly IWaveformService _waveform;
    private readonly IAudioPlayerService _player;
    private readonly ISettingsService _settings;
    private readonly SessionState _session;

    public TrackLoaderService(
        IWaveformService waveform,
        IAudioPlayerService player,
        ISettingsService settings,
        SessionState session)
    {
        _waveform = waveform;
        _player = player;
        _settings = settings;
        _session = session;
    }

    public IReadOnlyList<string> RecentFiles =>
        _settings.Current.RecentFiles.Where(File.Exists).ToList();

    public event EventHandler? RecentChanged;

    public async Task<AudioTrack> LoadAsync(string filePath, CancellationToken ct = default)
    {
        var track = await _waveform.LoadAsync(filePath, ct: ct);

        _session.CurrentTrack = track;
        _session.CurrentStemSet = null;
        _player.Load(filePath);

        AddRecent(filePath);
        return track;
    }

    public void RemoveRecent(string path)
    {
        var n = _settings.Current.RecentFiles.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        if (n > 0) { _settings.Save(); RecentChanged?.Invoke(this, EventArgs.Empty); }
    }

    private void AddRecent(string filePath)
    {
        var list = _settings.Current.RecentFiles;
        list.RemoveAll(p => string.Equals(p, filePath, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, filePath);
        if (list.Count > MaxRecent)
            list.RemoveRange(MaxRecent, list.Count - MaxRecent);

        _settings.Save();
        RecentChanged?.Invoke(this, EventArgs.Empty);
    }
}
