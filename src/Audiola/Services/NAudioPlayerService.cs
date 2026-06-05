using System.Windows.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Audiola.Services;

/// <summary>
/// Wiedergabe einer Audiodatei via NAudio. Ein <see cref="DispatcherTimer"/>
/// meldet regelmaessig die aktuelle Position fuer die UI. Ein
/// <see cref="LiveEqProcessor"/> hängt sich live in die Kette ein.
/// </summary>
public sealed class NAudioPlayerService : IAudioPlayerService
{
    private readonly LiveEqProcessor _liveEq;
    private readonly LiveFxProcessor _liveFx;
    private WaveOutEvent? _output;
    private AudioFileReader? _reader;
    private readonly DispatcherTimer _timer;
    private float _volume = 1.0f;

    public NAudioPlayerService(LiveEqProcessor liveEq, LiveFxProcessor liveFx)
    {
        _liveEq = liveEq;
        _liveFx = liveFx;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += (_, _) => PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    public PlayerState State { get; private set; } = PlayerState.Stopped;

    public TimeSpan Duration => _reader?.TotalTime ?? TimeSpan.Zero;

    public TimeSpan Position
    {
        get => _reader?.CurrentTime ?? TimeSpan.Zero;
        set
        {
            if (_reader is not null)
                _reader.CurrentTime = value;
        }
    }

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            if (_reader is not null)
                _reader.Volume = _volume;
        }
    }

    public event EventHandler? StateChanged;
    public event EventHandler? PositionChanged;

    public void Load(string filePath)
    {
        Stop();
        _reader?.Dispose();
        _output?.Dispose();

        _reader = new AudioFileReader(filePath) { Volume = _volume };
        _liveEq.Configure(_reader.WaveFormat.SampleRate);
        _liveFx.Configure(_reader.WaveFormat.SampleRate);

        ISampleProvider chain = new LiveEqSampleProvider(_reader, _liveEq);
        chain = new LiveFxSampleProvider(chain, _liveFx);
        _output = new WaveOutEvent();
        _output.Init(chain.ToWaveProvider());
        _output.PlaybackStopped += OnPlaybackStopped;

        SetState(PlayerState.Stopped);
    }

    public void Play()
    {
        if (_output is null) return;
        _output.Play();
        _timer.Start();
        SetState(PlayerState.Playing);
    }

    public void Pause()
    {
        if (_output is null) return;
        _output.Pause();
        _timer.Stop();
        SetState(PlayerState.Paused);
    }

    public void Stop()
    {
        _output?.Stop();
        _timer.Stop();
        if (_reader is not null)
            _reader.Position = 0;
        SetState(PlayerState.Stopped);
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        // Natuerliches Ende der Datei.
        if (_reader is not null && _reader.Position >= _reader.Length)
        {
            _reader.Position = 0;
            _timer.Stop();
            SetState(PlayerState.Stopped);
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SetState(PlayerState state)
    {
        if (State == state) return;
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _timer.Stop();
        if (_output is not null)
            _output.PlaybackStopped -= OnPlaybackStopped;
        _output?.Dispose();
        _reader?.Dispose();
    }
}
