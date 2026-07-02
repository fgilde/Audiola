using Audiola.Dsp;
using NAudio.Wave;

namespace Singola.Services;

/// <summary>
/// Die Karaoke-Bühne unter der Haube: spielt den Song ab und hört gleichzeitig auf
/// N Mikrofone (eines pro Spieler). Pro Spieler werden fortlaufend Tonhöhe (Hz) und
/// Pegel ermittelt — die UI pollt per <see cref="ReadPlayer"/> im Anzeige-Takt.
/// </summary>
public sealed class KaraokeEngine : IDisposable
{
    private AudioFileReader? _reader;
    private WaveOutEvent? _out;
    private readonly List<PlayerCapture> _captures = [];

    public bool IsPlaying => _out?.PlaybackState == PlaybackState.Playing;
    public double PositionSeconds => _reader?.CurrentTime.TotalSeconds ?? 0;
    public double DurationSeconds => _reader?.TotalTime.TotalSeconds ?? 0;
    public event EventHandler? PlaybackEnded;

    /// <summary>Startet Song + alle Spieler-Mikrofone (ein Capture je Gerät).</summary>
    public void Start(string songPath, IReadOnlyList<int> deviceNumbers)
    {
        Stop();

        _reader = new AudioFileReader(songPath);
        _out = new WaveOutEvent { DesiredLatency = 120 };
        _out.Init(_reader);
        _out.PlaybackStopped += (_, _) => PlaybackEnded?.Invoke(this, EventArgs.Empty);

        foreach (var dev in deviceNumbers)
            _captures.Add(new PlayerCapture(dev));

        _out.Play();
    }

    public void Pause() => _out?.Pause();
    public void Resume() => _out?.Play();

    public void Stop()
    {
        foreach (var c in _captures) c.Dispose();
        _captures.Clear();
        try { _out?.Stop(); } catch { }
        _out?.Dispose(); _out = null;
        _reader?.Dispose(); _reader = null;
    }

    /// <summary>Aktuelle Tonhöhe (0 = keine) und Spitzenpegel (0..1) des Spielers seit dem letzten Aufruf.</summary>
    public (float Hz, float Level) ReadPlayer(int index) =>
        index >= 0 && index < _captures.Count ? _captures[index].Read() : (0f, 0f);

    public void Dispose() => Stop();

    /// <summary>Mikrofon-Capture eines Spielers: Ringpuffer + Pitch-Erkennung auf den letzten ~46 ms.</summary>
    private sealed class PlayerCapture : IDisposable
    {
        private const int Rate = 44100;
        private readonly WaveInEvent _waveIn;
        private readonly float[] _ring = new float[8192];
        private int _write;
        private float _peak;
        private readonly object _lock = new();

        public PlayerCapture(int deviceNumber)
        {
            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(Rate, 16, 1),
                BufferMilliseconds = 30,
            };
            _waveIn.DataAvailable += OnData;
            _waveIn.StartRecording();
        }

        private void OnData(object? sender, WaveInEventArgs e)
        {
            lock (_lock)
            {
                for (var i = 0; i + 1 < e.BytesRecorded; i += 2)
                {
                    var s = BitConverter.ToInt16(e.Buffer, i) / 32768f;
                    _ring[_write] = s;
                    _write = (_write + 1) % _ring.Length;
                    var a = Math.Abs(s);
                    if (a > _peak) _peak = a;
                }
            }
        }

        public (float Hz, float Level) Read()
        {
            float peak;
            var window = new float[2048];   // ~46 ms — genug für Pitch ab ~90 Hz
            lock (_lock)
            {
                peak = _peak; _peak = 0;
                var start = _write - window.Length;
                for (var i = 0; i < window.Length; i++)
                    window[i] = _ring[((start + i) % _ring.Length + _ring.Length) % _ring.Length];
            }
            var hz = peak < 0.015f ? 0f : PitchDetector.DetectHz(window, Rate);
            return (hz, peak);
        }

        public void Dispose()
        {
            try { _waveIn.DataAvailable -= OnData; _waveIn.StopRecording(); } catch { }
            _waveIn.Dispose();
        }
    }
}
