namespace Audiola.Services;

/// <summary>
/// Gemeinsame Wiedergabe-Schnittstelle, damit die Transportleiste wahlweise den
/// Einzeldatei-Player (Original) oder die Stem-Mix-Engine steuern kann.
/// </summary>
public interface ITransportSource
{
    bool IsPlaying { get; }
    TimeSpan Position { get; set; }
    TimeSpan Duration { get; }

    event EventHandler? PositionChanged;
    event EventHandler? StateChanged;

    void Play();
    void Pause();
    void Stop();
}

/// <summary>Adapter für den Einzeldatei-Player.</summary>
public sealed class PlayerTransportSource : ITransportSource
{
    private readonly IAudioPlayerService _player;

    public PlayerTransportSource(IAudioPlayerService player)
    {
        _player = player;
        _player.PositionChanged += (s, e) => PositionChanged?.Invoke(s, e);
        _player.StateChanged += (s, e) => StateChanged?.Invoke(s, e);
    }

    public bool IsPlaying => _player.State == PlayerState.Playing;
    public TimeSpan Position { get => _player.Position; set => _player.Position = value; }
    public TimeSpan Duration => _player.Duration;

    public event EventHandler? PositionChanged;
    public event EventHandler? StateChanged;

    public void Play() => _player.Play();
    public void Pause() => _player.Pause();
    public void Stop() => _player.Stop();
}

/// <summary>Adapter für die Stem-Mix-Engine.</summary>
public sealed class MixerTransportSource : ITransportSource
{
    private readonly StemMixerEngine _engine;

    public MixerTransportSource(StemMixerEngine engine)
    {
        _engine = engine;
        _engine.PositionChanged += (s, e) => PositionChanged?.Invoke(s, e);
        _engine.StateChanged += (s, e) => StateChanged?.Invoke(s, e);
    }

    public bool IsPlaying => _engine.IsPlaying;
    public TimeSpan Position { get => _engine.Position; set => _engine.Position = value; }
    public TimeSpan Duration => _engine.Duration;

    public event EventHandler? PositionChanged;
    public event EventHandler? StateChanged;

    public void Play() => _engine.Play();
    public void Pause() => _engine.Pause();
    public void Stop() => _engine.Stop();
}
