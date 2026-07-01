using System.Windows.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Audiola.Services;

/// <summary>
/// Spielt zwei gleich lange Fassungen (Original + bearbeitet) synchron ab und blendet
/// verzögerungsfrei zwischen ihnen um — für einen echten A/B-Vergleich beim Spur-Mastern.
/// Beide laufen im selben Mixer; umgeschaltet wird nur die Lautstärke, damit die Position
/// exakt gleich bleibt und man den Unterschied ohne Sprung hört.
/// </summary>
public sealed class AbComparePlayer : IDisposable
{
    private WaveOutEvent? _output;
    private AudioFileReader? _a;
    private AudioFileReader? _b;
    private VolumeSampleProvider? _va;
    private VolumeSampleProvider? _vb;
    private readonly DispatcherTimer _timer;
    private bool _showB;

    public AbComparePlayer()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += (_, _) => PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool IsLoaded => _output is not null;
    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;
    public TimeSpan Duration => _a?.TotalTime ?? TimeSpan.Zero;

    public TimeSpan Position
    {
        get => _a?.CurrentTime ?? TimeSpan.Zero;
        set
        {
            if (_a is not null) _a.CurrentTime = value;
            if (_b is not null) _b.CurrentTime = value;
        }
    }

    /// <summary>true = bearbeitete (gemasterte) Fassung hörbar, false = Original.</summary>
    public bool ShowB
    {
        get => _showB;
        set
        {
            _showB = value;
            if (_va is not null) _va.Volume = value ? 0f : 1f;
            if (_vb is not null) _vb.Volume = value ? 1f : 0f;
        }
    }

    public event EventHandler? PositionChanged;
    public event EventHandler? StateChanged;

    public void Load(string originalPath, string processedPath)
    {
        Dispose();
        _a = new AudioFileReader(originalPath);
        _b = new AudioFileReader(processedPath);
        _va = new VolumeSampleProvider(_a) { Volume = _showB ? 0f : 1f };
        _vb = new VolumeSampleProvider(_b) { Volume = _showB ? 1f : 0f };
        var mix = new MixingSampleProvider([_va, _vb]) { ReadFully = true };
        _output = new WaveOutEvent();
        _output.Init(mix);
        _output.PlaybackStopped += OnStopped;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Play()
    {
        if (_output is null) return;
        _output.Play();
        _timer.Start();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Pause()
    {
        if (_output is null) return;
        _output.Pause();
        _timer.Stop();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void TogglePlay()
    {
        if (IsPlaying) Pause(); else Play();
    }

    private void OnStopped(object? sender, StoppedEventArgs e)
    {
        _timer.Stop();
        // Nur am tatsächlichen Ende zurückspulen (nicht bei Pause).
        if (_a is not null && _a.Position >= _a.Length) Position = TimeSpan.Zero;
        PositionChanged?.Invoke(this, EventArgs.Empty);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _timer.Stop();
        if (_output is not null)
        {
            _output.PlaybackStopped -= OnStopped;
            _output.Dispose();
            _output = null;
        }
        _a?.Dispose(); _b?.Dispose();
        _a = null; _b = null; _va = null; _vb = null;
    }
}
