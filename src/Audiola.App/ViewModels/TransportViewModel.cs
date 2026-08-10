using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Audiola.ViewModels;

public enum TransportMode
{
    Original,
    StemMix
}

/// <summary>
/// App-weite Transportleiste. Steuert kontextabhängig entweder den Originaltrack
/// oder den Stem-Mix – das aktive Ziel wird über <see cref="SetMode"/> umgeschaltet
/// (z. B. beim Wechsel auf die Seite „Stems &amp; Mix“).
/// </summary>
public sealed partial class TransportViewModel : ObservableObject
{
    private readonly SessionState _session;
    private readonly IAudioPlayerService _player;
    private readonly StemMixerEngine _engine;
    private readonly ITransportSource _original;
    private readonly ITransportSource _stemMix;
    private ITransportSource _current;
    private bool _suppressSeek;

    [ObservableProperty] private bool _hasTrack;
    [ObservableProperty] private string _trackName = "";
    [ObservableProperty] private string _modeLabel = "Original";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _positionText = "00:00";
    [ObservableProperty] private string _durationText = "00:00";
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private double _volume = 1.0;
    [ObservableProperty] private IReadOnlyList<float> _peaks = [];

    public TransportMode Mode { get; private set; } = TransportMode.Original;

    public TransportViewModel(IAudioPlayerService player, StemMixerEngine engine, SessionState session)
    {
        _session = session;
        _player = player;
        _engine = engine;
        _original = new PlayerTransportSource(player);
        _stemMix = new MixerTransportSource(engine);
        _current = _original;

        foreach (var src in new[] { _original, _stemMix })
        {
            src.PositionChanged += OnPositionChanged;
            src.StateChanged += OnStateChanged;
        }

        _session.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SessionState.CurrentTrack))
                AutoFollow();
        };
        _engine.Reloaded += (_, _) => AutoFollow();

        AutoFollow();
    }

    /// <summary>Schaltet das Steuerziel um (Studio-Mix oder Originaldatei).</summary>
    public void SetMode(TransportMode mode) => ApplyMode(mode);

    /// <summary>Folgt automatisch dem Studio-Mix, sobald Spuren existieren.</summary>
    private void AutoFollow()
    {
        var desired = _engine.Duration > TimeSpan.Zero ? TransportMode.StemMix
            : _session.CurrentTrack is not null ? TransportMode.Original
            : Mode;
        ApplyMode(desired);
    }

    private void ApplyMode(TransportMode mode)
    {
        if (Mode != mode)
        {
            _current.Pause();
            Mode = mode;
            _current = mode == TransportMode.StemMix ? _stemMix : _original;
        }
        UpdateMeta();
        RefreshFromCurrent();
        PlayPauseCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }

    private void UpdateMeta()
    {
        var track = _session.CurrentTrack;
        HasTrack = _engine.Duration > TimeSpan.Zero || track is not null;
        ModeLabel = Mode == TransportMode.StemMix ? "Studio" : "Original";
        TrackName = Mode == TransportMode.StemMix ? "Studio-Mix" : track?.FileName ?? "";
        Peaks = track?.Peaks ?? [];
    }

    private void RefreshFromCurrent()
    {
        IsPlaying = _current.IsPlaying;
        var dur = _current.Duration;
        DurationText = Format(dur);
        _suppressSeek = true;
        Progress = dur > TimeSpan.Zero ? _current.Position.TotalSeconds / dur.TotalSeconds : 0;
        _suppressSeek = false;
        PositionText = Format(_current.Position);
    }

    private void OnPositionChanged(object? sender, EventArgs e)
    {
        var dur = _current.Duration.TotalSeconds;
        _suppressSeek = true;
        Progress = dur > 0 ? _current.Position.TotalSeconds / dur : 0;
        _suppressSeek = false;
        PositionText = Format(_current.Position);
        DurationText = Format(_current.Duration);
    }

    private void OnStateChanged(object? sender, EventArgs e) => IsPlaying = _current.IsPlaying;

    partial void OnVolumeChanged(double value)
    {
        if (Mode == TransportMode.StemMix)
            _engine.MasterVolume = (float)value;   // Studio: Master
        else
            _player.Volume = (float)value;          // Originaldatei
    }

    partial void OnProgressChanged(double value)
    {
        if (_suppressSeek) return;
        var dur = _current.Duration;
        if (dur > TimeSpan.Zero)
            _current.Position = TimeSpan.FromSeconds(dur.TotalSeconds * Math.Clamp(value, 0, 1));
    }

    public void Seek(double ratio)
    {
        var dur = _current.Duration;
        if (dur > TimeSpan.Zero)
            _current.Position = TimeSpan.FromSeconds(dur.TotalSeconds * Math.Clamp(ratio, 0, 1));
    }

    private bool CanControl => HasTrack;

    [RelayCommand(CanExecute = nameof(CanControl))]
    private void PlayPause()
    {
        if (_current.IsPlaying) _current.Pause();
        else _current.Play();
    }

    [RelayCommand(CanExecute = nameof(CanControl))]
    private void Stop() => _current.Stop();

    private static string Format(TimeSpan t) => $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
}
