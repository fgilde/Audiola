namespace Audiola.Dsp;

/// <summary>
/// Biquad-Filter (Direct Form I) nach den RBJ-Audio-EQ-Cookbook-Formeln.
/// Wird sowohl fuer den Mastering-EQ als auch fuer das K-Weighting der
/// LUFS-Messung (BS.1770) verwendet.
/// </summary>
public sealed class Biquad
{
    private double _a0, _a1, _a2, _b0, _b1, _b2;
    private double _x1, _x2, _y1, _y2;

    public float Process(float input)
    {
        double x0 = input;
        double y0 = (_b0 / _a0) * x0
                  + (_b1 / _a0) * _x1
                  + (_b2 / _a0) * _x2
                  - (_a1 / _a0) * _y1
                  - (_a2 / _a0) * _y2;

        _x2 = _x1; _x1 = x0;
        _y2 = _y1; _y1 = y0;
        return (float)y0;
    }

    public void Reset()
    {
        _x1 = _x2 = _y1 = _y2 = 0;
    }

    /// <summary>Magnitudengang |H(e^jω)| in dB bei der angegebenen Frequenz.</summary>
    public double MagnitudeDb(double freqHz, double fs)
    {
        var w = 2 * Math.PI * freqHz / fs;
        double cw = Math.Cos(w), sw = Math.Sin(w), c2 = Math.Cos(2 * w), s2 = Math.Sin(2 * w);

        var numRe = _b0 + _b1 * cw + _b2 * c2;
        var numIm = -(_b1 * sw + _b2 * s2);
        var denRe = _a0 + _a1 * cw + _a2 * c2;
        var denIm = -(_a1 * sw + _a2 * s2);

        var num = Math.Sqrt(numRe * numRe + numIm * numIm);
        var den = Math.Sqrt(denRe * denRe + denIm * denIm);
        if (den < 1e-12) return 0;
        return 20 * Math.Log10(num / den);
    }

    public static Biquad HighPass(double fs, double fc, double q)
    {
        var f = new Biquad();
        var w0 = 2 * Math.PI * fc / fs;
        var cos = Math.Cos(w0);
        var alpha = Math.Sin(w0) / (2 * q);

        f._b0 = (1 + cos) / 2;
        f._b1 = -(1 + cos);
        f._b2 = (1 + cos) / 2;
        f._a0 = 1 + alpha;
        f._a1 = -2 * cos;
        f._a2 = 1 - alpha;
        return f;
    }

    public static Biquad Peaking(double fs, double fc, double q, double gainDb)
    {
        var f = new Biquad();
        var a = Math.Pow(10, gainDb / 40);
        var w0 = 2 * Math.PI * fc / fs;
        var cos = Math.Cos(w0);
        var alpha = Math.Sin(w0) / (2 * q);

        f._b0 = 1 + alpha * a;
        f._b1 = -2 * cos;
        f._b2 = 1 - alpha * a;
        f._a0 = 1 + alpha / a;
        f._a1 = -2 * cos;
        f._a2 = 1 - alpha / a;
        return f;
    }

    public static Biquad LowShelf(double fs, double fc, double q, double gainDb)
    {
        var f = new Biquad();
        var a = Math.Pow(10, gainDb / 40);
        var w0 = 2 * Math.PI * fc / fs;
        var cos = Math.Cos(w0);
        var alpha = Math.Sin(w0) / (2 * q);
        var twoSqrtAalpha = 2 * Math.Sqrt(a) * alpha;

        f._b0 = a * ((a + 1) - (a - 1) * cos + twoSqrtAalpha);
        f._b1 = 2 * a * ((a - 1) - (a + 1) * cos);
        f._b2 = a * ((a + 1) - (a - 1) * cos - twoSqrtAalpha);
        f._a0 = (a + 1) + (a - 1) * cos + twoSqrtAalpha;
        f._a1 = -2 * ((a - 1) + (a + 1) * cos);
        f._a2 = (a + 1) + (a - 1) * cos - twoSqrtAalpha;
        return f;
    }

    public static Biquad HighShelf(double fs, double fc, double q, double gainDb)
    {
        var f = new Biquad();
        var a = Math.Pow(10, gainDb / 40);
        var w0 = 2 * Math.PI * fc / fs;
        var cos = Math.Cos(w0);
        var alpha = Math.Sin(w0) / (2 * q);
        var twoSqrtAalpha = 2 * Math.Sqrt(a) * alpha;

        f._b0 = a * ((a + 1) + (a - 1) * cos + twoSqrtAalpha);
        f._b1 = -2 * a * ((a - 1) + (a + 1) * cos);
        f._b2 = a * ((a + 1) + (a - 1) * cos - twoSqrtAalpha);
        f._a0 = (a + 1) - (a - 1) * cos + twoSqrtAalpha;
        f._a1 = 2 * ((a - 1) - (a + 1) * cos);
        f._a2 = (a + 1) - (a - 1) * cos - twoSqrtAalpha;
        return f;
    }
}
