using System.Windows.Threading;
using Audiola.ViewModels;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Audiola.Services;

/// <summary>
/// Echtzeit-Wiedergabe mehrerer Stems als ein gemeinsamer Mix. Alle Stems laufen
/// sample-synchron; hörbar sind nur die aktiven (Enabled/Mute/Solo).
/// </summary>
public sealed class StemMixerEngine : IDisposable
{
    private readonly DispatcherTimer _timer;
    private WaveOutEvent? _output;
    private readonly List<AudioFileReader> _readers = [];
    private readonly List<ClipSampleProvider> _clips = [];
    private CountingSampleProvider? _counter;
    private VolumeSampleProvider? _master;
    private SpectrumSampleProvider? _spectrum;
    private readonly LiveEqProcessor _liveEq;
    private readonly LiveMasterProcessor _liveMaster;
    private float _masterVolume = 1f;
    private IReadOnlyList<StemTrackViewModel> _tracks = [];
    private int _sampleRate = 44100;
    private int _channels = 2;

    // ---- Echtzeit-Spektrum ----
    public const int SpectrumBands = 48;
    private const int FftSize = 1024;
    private readonly float[] _fftRe = new float[FftSize];
    private readonly float[] _fftIm = new float[FftSize];
    private readonly float[] _hann = new float[FftSize];
    private readonly float[] _mono = new float[FftSize];
    private readonly float[] _bands = new float[SpectrumBands];
    private int[] _bandOfBin = [];

    /// <summary>Wird im Wiedergabe-Takt mit den geglätteten Spektrum-Bändern (0..1) ausgelöst.</summary>
    public event EventHandler<float[]>? SpectrumUpdated;

    /// <summary>Master-Lautstärke (0..1.5), wirkt live auf die gesamte Wiedergabe.</summary>
    public float MasterVolume
    {
        get => _masterVolume;
        set { _masterVolume = value; if (_master is not null) _master.Volume = value; }
    }

    public StemMixerEngine(LiveEqProcessor liveEq, LiveMasterProcessor liveMaster)
    {
        _liveEq = liveEq;
        _liveMaster = liveMaster;
        for (var i = 0; i < FftSize; i++)
            _hann[i] = (float)(0.5 * (1 - Math.Cos(2 * Math.PI * i / (FftSize - 1))));

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += (_, _) =>
        {
            if (LoopEnabled && LoopEnd > LoopStart && Position >= LoopEnd)
                Position = LoopStart;
            UpdateMeters();
            UpdateSpectrum();
            PositionChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    public bool IsPlaying { get; private set; }
    public TimeSpan Duration { get; private set; }

    public bool LoopEnabled { get; set; }
    public TimeSpan LoopStart { get; set; }
    public TimeSpan LoopEnd { get; set; }

    public TimeSpan Position
    {
        get => _counter is null
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(_counter.SamplesRead / (double)(_sampleRate * _channels));
        set
        {
            var seconds = Math.Max(0, value.TotalSeconds);
            foreach (var clip in _clips)
                clip.SeekToOutputSeconds(seconds);
            _counter?.SetSamples((long)(seconds * _sampleRate) * _channels);
        }
    }

    public event EventHandler? StateChanged;
    public event EventHandler? PositionChanged;

    /// <summary>Wird ausgelöst, wenn die Spuren neu geladen/entladen wurden (Dauer geändert).</summary>
    public event EventHandler? Reloaded;

    /// <summary>
    /// Lädt die Stems als Spuren/Clips. Jede Spur startet bei ihrem
    /// <see cref="StemTrackViewModel.StartOffsetSeconds"/> (Stille davor).
    /// </summary>
    public void Load(IReadOnlyList<StemTrackViewModel> stems)
    {
        Unload();
        _tracks = stems;
        if (stems.Count == 0) { Reloaded?.Invoke(this, EventArgs.Empty); return; }

        var inputs = BuildInputs(stems, _readers, _clips, out _sampleRate, out _channels, out var dur);
        Duration = dur;

        var mixer = new MixingSampleProvider(inputs) { ReadFully = false };
        _master = new VolumeSampleProvider(mixer) { Volume = _masterVolume };
        _liveEq.Configure(_sampleRate);
        _liveMaster.Configure(_sampleRate);
        ISampleProvider chain = new LiveEqSampleProvider(_master, _liveEq);      // Master-EQ
        chain = new LiveMasterSampleProvider(chain, _liveMaster);                // Mastering-Vorschau
        _spectrum = new SpectrumSampleProvider(chain);                           // Tap für die Spektrum-Anzeige
        BuildBandMapping(_sampleRate);
        _counter = new CountingSampleProvider(_spectrum);
        _output = new WaveOutEvent();
        _output.Init(_counter.ToWaveProvider());
        _output.PlaybackStopped += OnPlaybackStopped;

        Reloaded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Rendert den Mix des Bereichs [start, end) offline (eigene Reader, ohne die
    /// Live-Wiedergabe zu stören) — für den Bereichs-Export.
    /// </summary>
    public (float[] Samples, int SampleRate) RenderRange(
        IReadOnlyList<StemTrackViewModel> tracks, TimeSpan start, TimeSpan end)
    {
        var readers = new List<AudioFileReader>();
        var clips = new List<ClipSampleProvider>();
        try
        {
            var inputs = BuildInputs(tracks, readers, clips, out var sr, out var ch, out _);
            var mixer = new MixingSampleProvider(inputs) { ReadFully = false };

            foreach (var c in clips) c.SeekToOutputSeconds(start.TotalSeconds);

            var totalSamples = (long)(Math.Max(0, (end - start).TotalSeconds) * sr) * ch;
            var outBuf = new float[totalSamples];
            var tmp = new float[sr * ch];
            long got = 0;
            while (got < totalSamples)
            {
                var toRead = (int)Math.Min(tmp.Length, totalSamples - got);
                var read = mixer.Read(tmp, 0, toRead);
                if (read == 0) break;
                Array.Copy(tmp, 0, outBuf, got, read);
                got += read;
            }
            return (outBuf, sr);
        }
        finally
        {
            foreach (var r in readers) r.Dispose();
        }
    }

    private static List<ISampleProvider> BuildInputs(
        IReadOnlyList<StemTrackViewModel> tracks,
        List<AudioFileReader> readers, List<ClipSampleProvider> clips,
        out int sampleRate, out int channels, out TimeSpan duration)
    {
        var inputs = new List<ISampleProvider>();
        var maxEnd = TimeSpan.Zero;
        var target = 0; // gemeinsame Projekt-Samplerate (von der ersten Quelle bestimmt)

        foreach (var vm in tracks)
        {
            if (vm.Clips.Count == 0)
            {
                if (string.IsNullOrEmpty(vm.Model.FilePath)) continue; // leere Spur (kein Material)
                AddClip(vm, null, vm.Model.FilePath, vm.StartOffsetSeconds, 0, -1);
            }
            else
                foreach (var clip in vm.Clips)
                {
                    var path = string.IsNullOrEmpty(clip.SourcePath) ? vm.Model.FilePath : clip.SourcePath;
                    AddClip(vm, clip, path, clip.TimelineOffsetSeconds, clip.SourceStartSeconds, clip.LengthSeconds);
                }
        }

        sampleRate = target == 0 ? 44100 : target;
        channels = 2;
        duration = maxEnd;
        return inputs;

        void AddClip(StemTrackViewModel vm, ClipViewModel? clip, string path, double offsetSec, double srcStartSec, double lenSec)
        {
            var reader = new AudioFileReader(path);
            readers.Add(reader);

            ISampleProvider provider = reader;
            if (provider.WaveFormat.Channels == 1)
                provider = new MonoToStereoSampleProvider(provider);

            var ch = provider.WaveFormat.Channels;
            var sr = provider.WaveFormat.SampleRate;
            if (target == 0) target = sr; // erste Quelle legt die Projekt-Rate fest
            var totalSec = reader.TotalTime.TotalSeconds;

            if (lenSec < 0) lenSec = totalSec - srcStartSec;
            lenSec = Math.Max(0, Math.Min(lenSec, totalSec - srcStartSec));

            long Samp(double s) => (long)(Math.Max(0, s) * sr) * ch;

            var clipProvider = new ClipSampleProvider(provider, reader, Samp(offsetSec), Samp(srcStartSec), Samp(lenSec), clip);
            clips.Add(clipProvider);

            ISampleProvider input = new LiveStemSampleProvider(clipProvider, vm, tracks);
            // Auf die Projekt-Rate resampeln, damit alle Mixer-Eingänge dasselbe Format haben.
            if (input.WaveFormat.SampleRate != target)
                input = new WdlResamplingSampleProvider(input, target);
            inputs.Add(input);

            var clipEnd = TimeSpan.FromSeconds(offsetSec + lenSec);
            if (clipEnd > maxEnd) maxEnd = clipEnd;
        }
    }

    public void Play()
    {
        if (_output is null) return;
        _output.Play();
        _timer.Start();
        SetPlaying(true);
    }

    public void Pause()
    {
        _output?.Pause();
        _timer.Stop();
        ZeroMeters();
        ZeroSpectrum();
        SetPlaying(false);
    }

    public void Stop()
    {
        _output?.Pause();
        _timer.Stop();
        Position = TimeSpan.Zero;
        ZeroMeters();
        ZeroSpectrum();
        SetPlaying(false);
        PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        _timer.Stop();
        SetPlaying(false);
    }

    private void SetPlaying(bool value)
    {
        if (IsPlaying == value) return;
        IsPlaying = value;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateMeters()
    {
        foreach (var t in _tracks)
        {
            var pk = t.MeterPeak;
            t.MeterPeak = 0;
            t.Level = pk > t.Level ? pk : t.Level * 0.8; // schneller Anstieg, langsamer Abfall
        }
    }

    private void ZeroMeters()
    {
        foreach (var t in _tracks) { t.MeterPeak = 0; t.Level = 0; }
    }

    // ---- Spektrum ----

    /// <summary>Ordnet jeden FFT-Bin (1..FftSize/2) logarithmisch einem Anzeige-Band zu.</summary>
    private void BuildBandMapping(int sampleRate)
    {
        var bins = FftSize / 2;
        _bandOfBin = new int[bins + 1];
        double minF = 30, maxF = Math.Min(sampleRate / 2.0, 18000);
        var logMin = Math.Log(minF);
        var logMax = Math.Log(maxF);
        for (var bin = 1; bin <= bins; bin++)
        {
            var f = bin * sampleRate / (double)FftSize;
            f = Math.Clamp(f, minF, maxF);
            var t = (Math.Log(f) - logMin) / (logMax - logMin);
            _bandOfBin[bin] = Math.Clamp((int)(t * SpectrumBands), 0, SpectrumBands - 1);
        }
    }

    private void UpdateSpectrum()
    {
        if (!IsPlaying || _spectrum is null || _bandOfBin.Length == 0)
            return;

        _spectrum.CopyLatest(_mono);
        for (var i = 0; i < FftSize; i++)
        {
            _fftRe[i] = _mono[i] * _hann[i];
            _fftIm[i] = 0f;
        }
        Audiola.Dsp.Fft.Forward(_fftRe, _fftIm);

        Span<float> peak = stackalloc float[SpectrumBands];
        for (var bin = 1; bin <= FftSize / 2; bin++)
        {
            var mag = MathF.Sqrt(_fftRe[bin] * _fftRe[bin] + _fftIm[bin] * _fftIm[bin]);
            var band = _bandOfBin[bin];
            if (mag > peak[band]) peak[band] = mag;
        }

        for (var b = 0; b < SpectrumBands; b++)
        {
            // In dB normalisieren (−60..0 dB → 0..1).
            var db = peak[b] > 1e-6f ? 20f * MathF.Log10(peak[b]) : -120f;
            var v = Math.Clamp((db + 60f) / 60f, 0f, 1f);
            _bands[b] = v > _bands[b] ? v : _bands[b] * 0.82f; // schneller rauf, langsam runter
        }

        SpectrumUpdated?.Invoke(this, _bands);
    }

    private void ZeroSpectrum()
    {
        Array.Clear(_bands);
        SpectrumUpdated?.Invoke(this, _bands);
    }

    private void Unload()
    {
        _timer.Stop();
        if (_output is not null)
        {
            _output.PlaybackStopped -= OnPlaybackStopped;
            _output.Dispose();
            _output = null;
        }
        foreach (var r in _readers)
            r.Dispose();
        ZeroMeters();
        _readers.Clear();
        _clips.Clear();
        _counter = null;
        _master = null;
        _spectrum = null;
        _tracks = [];
        IsPlaying = false;
        Duration = TimeSpan.Zero;
    }

    public void Dispose() => Unload();
}
