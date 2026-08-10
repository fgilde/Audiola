namespace Audiola.Services;

public enum PlayerState
{
    Stopped,
    Playing,
    Paused
}

/// <summary>Einfacher Single-File-Wiedergabedienst.</summary>
public interface IAudioPlayerService : IDisposable
{
    PlayerState State { get; }

    TimeSpan Position { get; set; }

    TimeSpan Duration { get; }

    float Volume { get; set; }

    event EventHandler? StateChanged;

    event EventHandler? PositionChanged;

    void Load(string filePath);

    void Play();

    void Pause();

    void Stop();
}
