using System.Windows.Threading;
using Audiola.Dsp;
using NAudio.Wave;

namespace Audiola.Services;

/// <summary>Ein live erkannter Ton beim Einsingen: Song-Zeit (s), Frequenz (Hz; 0 = stimmlos), Pegel (RMS 0..1).</summary>
public readonly record struct PitchSample(double TimeSeconds, float Hz, float Level);

/// <summary>
/// Kern des Einsing-Studios: spielt einen zuvor gerenderten Backing-Track ab und nimmt dazu synchron
/// das Mikrofon auf. Die Aufnahme wird um die kalibrierte Latenz zurückgeschoben (damit sie im Takt
/// sitzt) und in einen Puffer über die volle Songlänge geschrieben — so lassen sich einzelne Abschnitte
/// neu einsingen, ohne den Rest zu verlieren (Comping). Live liefert sie Tonhöhe + Pegel fürs Feedback.
///
/// Voraussetzung für sauberes Timing: Kopfhörer (sonst nimmt das Mikro den Backing-Track mit auf).
/// </summary>
public sealed class VocalRecordingEngine : IDisposable
{
    private const int Rate = 44100;
    private const int Win = 2048;   // Analysefenster für die Tonhöhe (~46 ms)

    private WaveOutEvent? _out;
    private AudioFileReader? _backing;
    private WaveInEvent? _mic;

    private float[] _vocal = [];    // Mono, volle Songlänge (44,1 kHz) — die gesammelte Aufnahme
    private long _writePos;         // aktuelle Schreibposition im Puffer (kann anfangs negativ sein)
    private readonly float[] _win = new float[Win];
    private int _winFill;

    private readonly DispatcherTimer _timer;

    public VocalRecordingEngine()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += (_, _) => PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Kalibrierter Round-Trip-Versatz (s), um den die Aufnahme zeitlich zurückgeschoben wird.</summary>
    public double LatencySeconds { get; set; }

    public bool IsRecording { get; private set; }
    public bool IsPlaying => _out?.PlaybackState == PlaybackState.Playing;
    public TimeSpan Position => _backing?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan Duration => _backing?.TotalTime ?? TimeSpan.Zero;
    public int SampleRate => Rate;

    /// <summary>Die bisher gesammelte Gesangsspur (Mono, 44,1 kHz) über die volle Länge.</summary>
    public float[] VocalBuffer => _vocal;

    public event EventHandler? PositionChanged;
    public event EventHandler? PlaybackEnded;
    public event EventHandler<PitchSample>? Pitch;

    /// <summary>Lädt den (extern gerenderten) Backing-Track und dimensioniert den Aufnahme-Puffer.</summary>
    public void LoadBacking(string wavPath)
    {
        DisposePlayback();
        _backing = new AudioFileReader(wavPath);
        _out = new WaveOutEvent();
        _out.Init(_backing);
        _out.PlaybackStopped += OnPlaybackStopped;

        int total = (int)(_backing.TotalTime.TotalSeconds * Rate) + Rate;
        if (_vocal.Length < total)
        {
            var bigger = new float[total];
            Array.Copy(_vocal, bigger, _vocal.Length);
            _vocal = bigger;
        }
    }

    /// <summary>Ab <paramref name="fromSec"/> abspielen; bei <paramref name="record"/> gleichzeitig aufnehmen.</summary>
    public void Play(double fromSec, bool record)
    {
        if (_backing is null || _out is null) return;
        Stop();
        _backing.CurrentTime = TimeSpan.FromSeconds(Math.Clamp(fromSec, 0, _backing.TotalTime.TotalSeconds));

        if (record)
        {
            // Erster aufgenommener Sample gehört – latenzkorrigiert – an diese Puffer-Position.
            _writePos = (long)Math.Round((fromSec - LatencySeconds) * Rate);
            _winFill = 0;
            _mic = new WaveInEvent { WaveFormat = new WaveFormat(Rate, 16, 1), BufferMilliseconds = 40 };
            _mic.DataAvailable += OnMic;
            _mic.StartRecording();
            IsRecording = true;
        }

        _out.Play();
        _timer.Start();
    }

    /// <summary>Playback + Aufnahme anhalten (Position bleibt erhalten).</summary>
    public void Stop()
    {
        _timer.Stop();
        _out?.Pause();
        if (_mic is not null)
        {
            _mic.DataAvailable -= OnMic;
            try { _mic.StopRecording(); } catch { /* schon gestoppt */ }
            _mic.Dispose();
            _mic = null;
        }
        IsRecording = false;
        PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnMic(object? sender, WaveInEventArgs e)
    {
        int n = e.BytesRecorded / 2;   // 16-bit mono
        for (int i = 0; i < n; i++)
        {
            short s = (short)(e.Buffer[i * 2] | (e.Buffer[i * 2 + 1] << 8));
            float f = s / 32768f;

            long pos = _writePos + i;
            if (pos >= 0 && pos < _vocal.Length) _vocal[pos] = f;

            _win[_winFill++] = f;
            if (_winFill >= Win)
            {
                float hz = PitchDetector.DetectHz(_win, Rate);
                float level = Rms(_win);
                double songT = (double)(_writePos + i) / Rate;  // latenzkorrigierte Song-Zeit
                Pitch?.Invoke(this, new PitchSample(songT, hz, level));
                _winFill = 0;
            }
        }
        _writePos += n;
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        // Nur beim tatsächlichen Ende (nicht bei Pause) melden.
        if (_backing is not null && _backing.Position >= _backing.Length)
        {
            Stop();
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    private static float Rms(float[] b)
    {
        double s = 0;
        for (int i = 0; i < b.Length; i++) s += b[i] * b[i];
        return (float)Math.Sqrt(s / b.Length);
    }

    /// <summary>Setzt die gesammelte Aufnahme zurück (z. B. für einen kompletten Neustart).</summary>
    public void ClearVocal() => Array.Clear(_vocal);

    private void DisposePlayback()
    {
        if (_out is not null) { _out.PlaybackStopped -= OnPlaybackStopped; _out.Dispose(); _out = null; }
        _backing?.Dispose(); _backing = null;
    }

    public void Dispose()
    {
        Stop();
        DisposePlayback();
    }
}
