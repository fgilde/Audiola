using Audiola.Dsp;
using Audiola.Services.Audio;
using NAudio.Wave;

namespace Singola.Services;

/// <summary>
/// Platform-neutral karaoke transport and live microphone analysis. Audio I/O is
/// supplied by the Avalonia host through <see cref="IAudioPlatform"/>.
/// </summary>
public sealed class KaraokeEngine : IDisposable
{
    private readonly IAudioPlatform _audio;
    private IAudioPlayback? _playback;
    private readonly List<PlayerCapture?> _captures = [];
    private readonly List<string?> _captureErrors = [];

    public KaraokeEngine(IAudioPlatform audio) => _audio = audio;

    public bool IsPlaying => _playback?.IsPlaying == true;
    public double PositionSeconds => _playback?.Position.TotalSeconds ?? 0;
    public double DurationSeconds => _playback?.Duration.TotalSeconds ?? 0;
    public event EventHandler? PlaybackEnded;

    /// <summary>
    /// Starts song playback and one microphone capture stream per player. When
    /// <paramref name="recordPaths"/> is given, each player's microphone is written to that
    /// WAV file so the round can be exported afterwards.
    /// </summary>
    public void Start(string songPath, IReadOnlyList<string> inputDeviceIds, IReadOnlyList<string>? recordPaths = null)
    {
        Stop();

        var playback = _audio.CreatePlayback();
        playback.PlaybackEnded += OnPlaybackEnded;
        try
        {
            playback.Open(songPath);
            OpenCaptures(inputDeviceIds, recordPaths);
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

    /// <summary>
    /// Opens the microphones without a song — the level meters in the setup screen use this so
    /// players can verify their microphone before the round starts.
    /// </summary>
    public void StartMonitor(IReadOnlyList<string> inputDeviceIds)
    {
        Stop();
        OpenCaptures(inputDeviceIds, null);
    }

    /// <summary>Why a player's microphone could not be opened, or <c>null</c> when it works.</summary>
    public string? CaptureError(int index) =>
        index >= 0 && index < _captureErrors.Count ? _captureErrors[index] : null;

    /// <summary>
    /// A microphone that fails to open must not abort the round: the other players keep singing
    /// and the affected slot shows why its microphone is silent.
    /// </summary>
    private void OpenCaptures(IReadOnlyList<string> inputDeviceIds, IReadOnlyList<string>? recordPaths)
    {
        for (var index = 0; index < inputDeviceIds.Count; index++)
        {
            PlayerCapture? capture = null;
            try
            {
                capture = new PlayerCapture(
                    _audio.CreateCapture(inputDeviceIds[index]),
                    recordPaths is null ? null : recordPaths.ElementAtOrDefault(index));
                capture.Start();
                _captures.Add(capture);
                _captureErrors.Add(null);
            }
            catch (Exception ex)
            {
                // Sonst bliebe die Mitschnitt-Datei offen und die nächste Runde könnte sie nicht neu anlegen.
                capture?.Dispose();
                _captures.Add(null);
                _captureErrors.Add(ex.Message);
            }
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
        index >= 0 && index < _captures.Count ? _captures[index]?.Read() ?? (0f, 0f) : (0f, 0f);

    public void Dispose() => Stop();

    private void OnPlaybackEnded(object? sender, EventArgs e) => PlaybackEnded?.Invoke(this, e);

    private void StopCaptures()
    {
        foreach (var capture in _captures) capture?.Dispose();
        _captures.Clear();
        _captureErrors.Clear();
    }

    /// <summary>Ring-buffered, mono 44.1 kHz microphone analysis matching the prior scoring window.</summary>
    private sealed class PlayerCapture : IDisposable
    {
        private const int Rate = 44100;
        private readonly IAudioCapture _capture;
        private readonly float[] _ring = new float[8192];
        private readonly object _sync = new();
        private WaveFileWriter? _writer;
        private int _write;
        private float _peak;

        public PlayerCapture(IAudioCapture capture, string? recordPath)
        {
            _capture = capture;
            if (recordPath is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(recordPath)!);
                // ponytail: Schreiben passiert im Audio-Callback. Bei 44,1 kHz mono puffert der
                // FileStream das weg; falls je Aussetzer auftreten, hier eine Queue dazwischen.
                _writer = new WaveFileWriter(recordPath, new WaveFormat(Rate, 16, 1));
            }
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
            lock (_sync)
            {
                _writer?.Dispose();
                _writer = null;
            }
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
                    _writer?.WriteSample(sample);
                }
            }
        }
    }
}
