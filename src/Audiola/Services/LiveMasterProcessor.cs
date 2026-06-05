using Audiola.Dsp;
using Audiola.Models;

namespace Audiola.Services;

/// <summary>
/// Streamingfähige Master-Kette (EQ → Kompressor → Ausgangs-Gain) für die
/// Mastering-Vorschau im Studio. Lässt sich live an/aus schalten (A/B-Vergleich).
/// Die integrierte LUFS-Normalisierung der Offline-Kette wird hier durch einen
/// statischen Ausgangs-Gain angenähert (vom ViewModel aus der Messung gesetzt).
/// </summary>
public sealed class LiveMasterProcessor
{
    private readonly object _gate = new();
    private MasteringSettings? _settings;
    private float _outGain = 1f;
    private int _sampleRate = 44100;
    private int _channels = 2;
    private bool _enabled;
    private bool _dirty;

    private Biquad[][] _eq = [];
    private Compressor? _comp;

    public bool Enabled
    {
        get { lock (_gate) return _enabled; }
        set { lock (_gate) { _enabled = value; _dirty = true; } }
    }

    public void Configure(int sampleRate)
    {
        lock (_gate) { _sampleRate = sampleRate; _dirty = true; }
    }

    public void SetSettings(MasteringSettings settings, double outputGainDb)
    {
        lock (_gate)
        {
            _settings = settings;
            _outGain = (float)Math.Pow(10, outputGainDb / 20);
            _dirty = true;
        }
    }

    public void Process(float[] buffer, int offset, int count, int channels)
    {
        lock (_gate)
        {
            if (!_enabled || _settings is null) return;
            if (_dirty || _channels != channels) Rebuild(channels);

            for (var i = 0; i < count; i += channels)
            {
                // EQ pro Kanal.
                for (var c = 0; c < channels && i + c < count; c++)
                {
                    var s = buffer[offset + i + c];
                    foreach (var f in _eq[c]) s = f.Process(s);
                    buffer[offset + i + c] = s;
                }

                // Kompressor (stereo gekoppelt).
                if (_comp is not null && channels == 2 && i + 1 < count)
                {
                    var l = buffer[offset + i];
                    var r = buffer[offset + i + 1];
                    _comp.Process(ref l, ref r);
                    buffer[offset + i] = l;
                    buffer[offset + i + 1] = r;
                }

                // Ausgangs-Gain (Loudness-Näherung) + Clipping-Schutz.
                for (var c = 0; c < channels && i + c < count; c++)
                {
                    var v = buffer[offset + i + c] * _outGain;
                    buffer[offset + i + c] = Math.Clamp(v, -1f, 1f);
                }
            }
        }
    }

    private void Rebuild(int channels)
    {
        _channels = channels;
        var s = _settings!;
        _eq = new Biquad[channels][];
        for (var c = 0; c < channels; c++)
            _eq[c] = BuildEq(s, _sampleRate).ToArray();

        _comp = s.CompressorEnabled
            ? new Compressor(_sampleRate, s.ThresholdDb, s.Ratio, s.AttackMs, s.ReleaseMs, s.MakeupGainDb)
            : null;
        _dirty = false;
    }

    private static List<Biquad> BuildEq(MasteringSettings s, int fs)
    {
        var chain = new List<Biquad>();
        if (s.HighPassEnabled) chain.Add(Biquad.HighPass(fs, s.HighPassHz, 0.707));
        if (Math.Abs(s.LowShelfGainDb) > 0.01) chain.Add(Biquad.LowShelf(fs, s.LowShelfHz, 0.707, s.LowShelfGainDb));
        if (Math.Abs(s.MidGainDb) > 0.01) chain.Add(Biquad.Peaking(fs, s.MidHz, s.MidQ, s.MidGainDb));
        if (Math.Abs(s.HighShelfGainDb) > 0.01) chain.Add(Biquad.HighShelf(fs, s.HighShelfHz, 0.707, s.HighShelfGainDb));
        return chain;
    }
}
