using Audiola.Dsp;
using Audiola.Services.Audio;

namespace Singola.Services;

/// <summary>
/// Platform-neutral karaoke transport and live microphone analysis. Audio I/O is
/// supplied by the Avalonia host through <see cref="IAudioPlatform"/>.
/// </summary>
public sealed class KaraokeEngine : IDisposable
{
    private readonly IAudioPlatform _audio;
    private IAudioPlayback? _playback;
    private readonly List<PlayerCapture> _captures = [];

    public KaraokeEngine(IAudioPlatform audio) => _audio = audio;

    public bool IsPlaying => _playback?.IsPlaying == true;
    public double PositionSeconds => _playback?.Position.TotalSeconds ?? 0;
    public double DurationSeconds => _playback?.Duration.TotalSeconds ?? 0;
    public event EventHandler? PlaybackEnded;

    /// <summary>Starts song playback and one microphone capture stream per player.</summary>
    public void Start(string songPath, IReadOnlyList<string> inputDeviceIds)
    {
        Stop();

        var playback = _audio.CreatePlayback();
        playback.PlaybackEnded += OnPlaybackEnded;
        try
        {
            playback.Open(songPath);
            foreach (var deviceId in inputDeviceIds)
                _captures.Add(new PlayerCapture(_audio.CreateCapture(deviceId)));

            foreach (var capture in _captures) capture.Start();
            playback.Play();
            _playback = playback;
        }
        catch
        {
            playback.PlaybackEnded -= OnPlaybackEnded;
            playback.Dispose();
            StopCaptures();
            throw;
        }
    }

    public void Pause() => _playback?.Pause();
    public void Resume() => _playback?.Play();
    public void Seek(TimeSpan position) => _playback?.Seek(position);

    public void Stop()
    {
        StopCaptures();

        if (_playback is { } playback)
        {
            playback.PlaybackEnded -= OnPlaybackEnded;
            playback.Stop();
            playback.Dispose();
            _playback = null;
        }
    }

    /// <summary>Current pitch (0 = none) and peak level since the last call.</summary>
    public (float Hz, float Level) ReadPlayer(int index) =>
        index >= 0 && index < _captures.Count ? _captures[index].Read() : (0f, 0f);

    public void Dispose() => Stop();

    private void OnPlaybackEnded(object? sender, EventArgs e) => PlaybackEnded?.Invoke(this, e);

    private void StopCaptures()
    {
        foreach (var capture in _captures) capture.Dispose();
        _captures.Clear();
    }

    /// <summary>Ring-buffered, mono 44.1 kHz microphone analysis matching the prior scoring window.</summary>
    private sealed class PlayerCapture : IDisposable
    {
        private const int Rate = 44100;
        private readonly IAudioCapture _capture;
        private readonly float[] _ring = new float[8192];
        private readonly object _sync = new();
        private int _write;
        private float _peak;

        public PlayerCapture(IAudioCapture capture)
        {
            _capture = capture;
            _capture.SamplesAvailable += OnSamplesAvailable;
        }

        public void Start() => _capture.Start();

        public (float Hz, float Level) Read()
        {
            float peak;
            var window = new float[2048];
            lock (_sync)
            {
                peak = _peak;
                _peak = 0;
                var start = _write - window.Length;
                for (var i = 0; i < window.Length; i++)
                    window[i] = _ring[((start + i) % _ring.Length + _ring.Length) % _ring.Length];
            }

            var hz = peak < 0.015f ? 0f : PitchDetector.DetectHz(window, Rate);
            return (hz, peak);
        }

        public void Dispose()
        {
            _capture.SamplesAvailable -= OnSamplesAvailable;
            _capture.Dispose();
        }

        private void OnSamplesAvailable(object? sender, AudioSamplesEventArgs e)
        {
            if (e.SampleRate != Rate || e.Channels < 1) return;

            lock (_sync)
            {
                var samples = e.Samples.Span;
                for (var i = 0; i + e.Channels <= samples.Length; i += e.Channels)
                {
                    var sample = 0f;
                    for (var channel = 0; channel < e.Channels; channel++) sample += samples[i + channel];
                    sample /= e.Channels;

                    _ring[_write] = sample;
                    _write = (_write + 1) % _ring.Length;
                    var magnitude = Math.Abs(sample);
                    if (magnitude > _peak) _peak = magnitude;
                }
            }
        }
    }
}
