namespace Audiola.Services;

/// <summary>
/// Geteilte Live-Effektkette für die Wiedergabe (Stereo verbreitern → Echo → Hall).
/// Anders als <see cref="AudioEffects"/> hält dieser Prozessor seinen Zustand über
/// Blöcke hinweg (Delay-Lines, Reverb-Buffer), damit Effekte beim Abspielen
/// kontinuierlich klingen. Thread-sicher via Lock + Dirty-Flag.
/// </summary>
public sealed class LiveFxProcessor
{
    private const double MaxDelaySec = 2.0;

    private readonly object _gate = new();
    private bool _enabled;
    private bool _echo, _reverb, _widen;
    private double _echoDelayMs = 300, _echoFeedback = 0.4, _echoMix = 0.5, _reverbMix = 0.3, _widenAmount = 1.6;

    private int _sampleRate = 44100;
    private int _channels = 2;
    private bool _dirty = true;

    private float[][] _echoLine = [];
    private int[] _echoPos = [];
    private ReverbState[] _reverbState = [];

    public bool Enabled
    {
        get { lock (_gate) return _enabled; }
        set { lock (_gate) { if (value && !_enabled) _dirty = true; _enabled = value; } }
    }

    public void Configure(int sampleRate)
    {
        lock (_gate) { _sampleRate = sampleRate; _dirty = true; }
    }

    public void SetParams(bool echo, bool reverb, bool widen,
        double echoDelayMs, double echoFeedback, double echoMix, double reverbMix, double widenAmount)
    {
        lock (_gate)
        {
            _echo = echo; _reverb = reverb; _widen = widen;
            _echoDelayMs = echoDelayMs; _echoFeedback = echoFeedback; _echoMix = echoMix;
            _reverbMix = reverbMix; _widenAmount = widenAmount;
        }
    }

    public void Process(float[] buffer, int offset, int count, int channels)
    {
        lock (_gate)
        {
            if (!_enabled || (!_echo && !_reverb && !_widen)) return;

            if (_dirty || _channels != channels)
            {
                _channels = channels;
                Allocate();
                _dirty = false;
            }

            var delaySamples = Math.Clamp((int)(_echoDelayMs * 0.001 * _sampleRate), 1, _echoLine[0].Length - 1);

            for (var i = 0; i < count; i += channels)
            {
                // Stereo verbreitern (nur sinnvoll bei 2 Kanälen).
                if (_widen && channels == 2 && i + 1 < count)
                {
                    var l = buffer[offset + i];
                    var r = buffer[offset + i + 1];
                    var mid = (l + r) * 0.5f;
                    var side = (l - r) * 0.5f * (float)_widenAmount;
                    buffer[offset + i] = mid + side;
                    buffer[offset + i + 1] = mid - side;
                }

                for (var c = 0; c < channels && i + c < count; c++)
                {
                    var x = buffer[offset + i + c];

                    if (_echo)
                    {
                        var line = _echoLine[c];
                        var w = _echoPos[c];
                        var read = (w - delaySamples + line.Length) % line.Length;
                        var wet = line[read];
                        x = (float)(x + _echoMix * wet);
                        line[w] = (float)(buffer[offset + i + c] + _echoFeedback * wet);
                        _echoPos[c] = (w + 1) % line.Length;
                    }

                    if (_reverb)
                        x = _reverbState[c].Process(x, (float)_reverbMix);

                    buffer[offset + i + c] = x;
                }
            }
        }
    }

    private void Allocate()
    {
        var lineLen = Math.Max(1, (int)(MaxDelaySec * _sampleRate));
        _echoLine = new float[_channels][];
        _echoPos = new int[_channels];
        _reverbState = new ReverbState[_channels];
        for (var c = 0; c < _channels; c++)
        {
            _echoLine[c] = new float[lineLen];
            _reverbState[c] = new ReverbState(_sampleRate);
        }
    }

    /// <summary>Persistenter Schroeder-Reverb (4 Kamm- + 2 Allpassfilter) für einen Kanal.</summary>
    private sealed class ReverbState
    {
        private static readonly int[] CombDelays = [1557, 1617, 1491, 1422];
        private static readonly double[] CombGains = [0.805, 0.827, 0.783, 0.764];
        private static readonly int[] ApDelays = [225, 556];
        private const double ApGain = 0.5;

        private readonly float[][] _comb;
        private readonly int[] _combPos;
        private readonly float[][] _ap;
        private readonly int[] _apPos;

        public ReverbState(int sampleRate)
        {
            var scale = sampleRate / 44100.0;
            _comb = CombDelays.Select(d => new float[Math.Max(1, (int)(d * scale))]).ToArray();
            _combPos = new int[CombDelays.Length];
            _ap = ApDelays.Select(d => new float[Math.Max(1, (int)(d * scale))]).ToArray();
            _apPos = new int[ApDelays.Length];
        }

        public float Process(float dry, float mix)
        {
            float combSum = 0;
            for (var k = 0; k < _comb.Length; k++)
            {
                var buf = _comb[k];
                var p = _combPos[k];
                var y = buf[p];
                buf[p] = dry + (float)(CombGains[k] * y);
                _combPos[k] = (p + 1) % buf.Length;
                combSum += y;
            }
            combSum /= _comb.Length;

            var sig = combSum;
            for (var k = 0; k < _ap.Length; k++)
            {
                var buf = _ap[k];
                var p = _apPos[k];
                var bufVal = buf[p];
                var outv = (float)(-ApGain * sig + bufVal);
                buf[p] = (float)(sig + ApGain * outv);
                _apPos[k] = (p + 1) % buf.Length;
                sig = outv;
            }

            return dry * (1 - mix) + sig * mix;
        }
    }
}
