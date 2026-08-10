using Audiola.ViewModels;
using NAudio.Wave;

namespace Audiola.Services;

/// <summary>
/// Echtzeit-Binaural-Vorschau: lädt die Quell-Spuren als Mono-Puffer und mischt sie
/// live zu Stereo. Position/Distanz/Pegel werden in jedem Audio-Block frisch aus den
/// <see cref="SpatialSourceViewModel"/> gelesen — Schieberegler wirken also sofort hörbar.
/// </summary>
public sealed class SpatialPreviewEngine : IDisposable
{
    private const int Sr = 48000;
    private WaveOutEvent? _out;

    public bool IsPlaying { get; private set; }
    public event EventHandler? Stopped;

    public async Task StartAsync(IReadOnlyList<SpatialSourceViewModel> sources)
    {
        Stop();
        var snapshot = sources.ToList();
        var monos = await Task.Run(() => LoadMonos(snapshot));
        if (snapshot.Count == 0 || monos.All(m => m.Length == 0)) return;

        var provider = new SpatialMixProvider(snapshot, monos, Sr);
        _out = new WaveOutEvent { DesiredLatency = 160 };
        _out.PlaybackStopped += OnStopped;
        _out.Init(provider);
        _out.Play();
        IsPlaying = true;
    }

    private void OnStopped(object? sender, StoppedEventArgs e)
    {
        IsPlaying = false;
        Stopped?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        if (_out is not null)
        {
            _out.PlaybackStopped -= OnStopped;
            try { _out.Stop(); } catch { /* egal */ }
            _out.Dispose();
            _out = null;
        }
        IsPlaying = false;
    }

    public void Dispose() => Stop();

    private static float[][] LoadMonos(List<SpatialSourceViewModel> sources)
    {
        var monos = new float[sources.Count][];
        for (var i = 0; i < sources.Count; i++)
        {
            try
            {
                var (st, sr) = AudioProcessingHelper.ReadStereo(sources[i].FilePath);
                if (sr != Sr) st = AudioProcessingHelper.Resample(st, sr, Sr);
                var frames = st.Length / 2;
                var mono = new float[frames];
                for (var n = 0; n < frames; n++) mono[n] = (st[n * 2] + st[n * 2 + 1]) * 0.5f;
                monos[i] = mono;
            }
            catch { monos[i] = []; }
        }
        return monos;
    }

    /// <summary>Mischt alle Quellen live zu einem binauralen Stereo-Strom.</summary>
    private sealed class SpatialMixProvider : ISampleProvider
    {
        private readonly SpatialSourceViewModel[] _vm;
        private readonly float[][] _mono;
        private readonly int _sr;
        private readonly int _len;
        private readonly int _maxItd;
        private readonly double[] _lpFar;
        private long _pos;

        public WaveFormat WaveFormat { get; }

        public SpatialMixProvider(List<SpatialSourceViewModel> vm, float[][] mono, int sr)
        {
            _vm = vm.ToArray();
            _mono = mono;
            _sr = sr;
            _maxItd = (int)(0.0007 * sr);
            _lpFar = new double[_vm.Length];
            foreach (var m in mono) if (m.Length > _len) _len = m.Length;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sr, 2);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var frames = count / 2;
            var avail = (int)Math.Min(frames, Math.Max(0, _len - _pos));
            if (avail <= 0) return 0;

            var n = _vm.Length;
            var gL = new double[n]; var gR = new double[n]; var alpha = new double[n];
            var delay = new int[n]; var farLeft = new bool[n]; var active = new bool[n];

            for (var s = 0; s < n; s++)
            {
                var v = _vm[s];
                active[s] = !v.Muted && _mono[s].Length > 0;
                if (!active[s]) continue;

                var az = v.AzimuthDeg * Math.PI / 180.0;
                var el = v.ElevationDeg * Math.PI / 180.0;
                var sinAz = Math.Sin(az);
                var elF = Math.Cos(el);
                var distGain = Math.Clamp(1.0 / Math.Clamp(v.Distance, 0.3, 3.0), 0.0, 1.5);
                var g = Math.Pow(10, v.GainDb / 20.0) * distGain;
                var itd = _maxItd * sinAz * elF;
                delay[s] = (int)Math.Round(Math.Abs(itd));
                farLeft[s] = sinAz > 0;
                var ildHalf = 4.5 * sinAz * elF;
                gL[s] = g * Math.Pow(10, -ildHalf / 20.0);
                gR[s] = g * Math.Pow(10, +ildHalf / 20.0);
                alpha[s] = Math.Exp(-2 * Math.PI * (19000 - 14000 * Math.Abs(sinAz)) / _sr);
            }

            for (var i = 0; i < avail; i++)
            {
                var p = _pos + i;
                double outL = 0, outR = 0;
                for (var s = 0; s < n; s++)
                {
                    if (!active[s]) continue;
                    var m = _mono[s];
                    if (p >= m.Length) continue;
                    var near = m[p];
                    var fp = p - delay[s];
                    var farIn = fp >= 0 && fp < m.Length ? m[fp] : 0f;
                    _lpFar[s] = (1 - alpha[s]) * farIn + alpha[s] * _lpFar[s];
                    var far = (float)_lpFar[s];
                    if (farLeft[s]) { outR += gR[s] * near; outL += gL[s] * far; }
                    else { outL += gL[s] * near; outR += gR[s] * far; }
                }
                // Sanfte Sättigung statt hartem Clipping (verhindert Knackser bei Summen > 1).
                buffer[offset + i * 2] = (float)Math.Tanh(outL * 0.9);
                buffer[offset + i * 2 + 1] = (float)Math.Tanh(outR * 0.9);
            }

            _pos += avail;
            return avail * 2;
        }
    }
}
