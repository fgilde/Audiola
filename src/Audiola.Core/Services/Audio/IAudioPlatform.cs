namespace Audiola.Services.Audio;

/// <summary>Identifies an audio-input device without exposing an OS-specific handle.</summary>
public sealed record AudioInputDevice(string Id, string Name, bool IsDefault = false)
{
    public override string ToString() => Name;
}

/// <summary>Interleaved, normalized PCM audio delivered by an input device.</summary>
public sealed class AudioSamplesEventArgs(ReadOnlyMemory<float> samples, int sampleRate, int channels) : EventArgs
{
    public ReadOnlyMemory<float> Samples { get; } = samples;
    public int SampleRate { get; } = sampleRate;
    public int Channels { get; } = channels;
}

/// <summary>Host-implemented playback with transport controls suitable for a timeline.</summary>
public interface IAudioPlayback : IDisposable
{
    event EventHandler? PlaybackEnded;

    bool IsPlaying { get; }
    TimeSpan Position { get; }
    TimeSpan Duration { get; }

    void Open(string path);
    void Play();
    void Pause();
    void Stop();
    void Seek(TimeSpan position);
}

/// <summary>Host-implemented real-time microphone capture.</summary>
public interface IAudioCapture : IDisposable
{
    event EventHandler<AudioSamplesEventArgs>? SamplesAvailable;

    bool IsRecording { get; }

    void Start();
    void Stop();
}

/// <summary>
/// Boundary between the shared karaoke/scoring code and platform-native audio I/O.
/// Hosts supply a backend for their supported operating systems.
/// </summary>
public interface IAudioPlatform : IDisposable
{
    IReadOnlyList<AudioInputDevice> GetInputDevices();
    IAudioPlayback CreatePlayback();
    IAudioCapture CreateCapture(string? inputDeviceId = null);
}
