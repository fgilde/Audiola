namespace Audiola.Dsp;

/// <summary>
/// Einfacher Feed-Forward-Kompressor mit Stereo-gekoppelter Pegelerkennung
/// (Peak-Detektor mit Attack/Release), harter Knie-Kennlinie und Makeup-Gain.
/// </summary>
public sealed class Compressor
{
    private readonly double _thresholdDb;
    private readonly double _ratio;
    private readonly double _attackCoef;
    private readonly double _releaseCoef;
    private readonly double _makeupLin;
    private double _envelope;

    public Compressor(int sampleRate, double thresholdDb, double ratio,
        double attackMs, double releaseMs, double makeupGainDb)
    {
        _thresholdDb = thresholdDb;
        _ratio = Math.Max(1.0, ratio);
        _attackCoef = Math.Exp(-1.0 / (Math.Max(0.1, attackMs) * 0.001 * sampleRate));
        _releaseCoef = Math.Exp(-1.0 / (Math.Max(0.1, releaseMs) * 0.001 * sampleRate));
        _makeupLin = Math.Pow(10, makeupGainDb / 20);
    }

    /// <summary>Verarbeitet ein Stereo-Sample-Paar (gekoppelte Pegelreduktion).</summary>
    public void Process(ref float left, ref float right)
    {
        var peak = Math.Max(Math.Abs(left), Math.Abs(right));

        // Hüllkurve (Peak-Detektor).
        var coef = peak > _envelope ? _attackCoef : _releaseCoef;
        _envelope = coef * _envelope + (1 - coef) * peak;

        var levelDb = 20 * Math.Log10(_envelope + 1e-12);
        var gainDb = 0.0;
        if (levelDb > _thresholdDb)
            gainDb = (_thresholdDb - levelDb) * (1 - 1 / _ratio);

        var gain = Math.Pow(10, gainDb / 20) * _makeupLin;
        left = (float)(left * gain);
        right = (float)(right * gain);
    }
}
