using Audiola.Dsp;

namespace Audiola.Services;

/// <summary>
/// Audio-Effekte auf interleaved Stereo-Float-Puffern. Alle Methoden sind rein:
/// sie arbeiten auf einer Kopie und lassen die Länge unverändert (Effekte wirken
/// im Bereich [startFrame, endFrame)).
/// </summary>
public static class AudioEffects
{
    private const int Ch = 2;

    private static (int, int) Clamp(float[] s, int a, int b)
    {
        var frames = s.Length / Ch;
        a = Math.Clamp(a, 0, frames);
        b = Math.Clamp(b, 0, frames);
        if (b < a) (a, b) = (b, a);
        return (a, b);
    }

    /// <summary>Peak-Normalisierung auf Ziel-dBFS (Standard −1 dBFS).</summary>
    public static float[] Normalize(float[] samples, int a, int b, double targetDb = -1.0)
    {
        (a, b) = Clamp(samples, a, b);
        var result = (float[])samples.Clone();

        var peak = 0f;
        for (var i = a * Ch; i < b * Ch; i++)
            peak = Math.Max(peak, Math.Abs(result[i]));
        if (peak <= 1e-9f) return result;

        var target = (float)Math.Pow(10, targetDb / 20);
        var gain = target / peak;
        for (var i = a * Ch; i < b * Ch; i++)
            result[i] *= gain;
        return result;
    }

    /// <summary>Kehrt den Bereich um (rückwärts).</summary>
    public static float[] Reverse(float[] samples, int a, int b)
    {
        (a, b) = Clamp(samples, a, b);
        var result = (float[])samples.Clone();
        int lo = a, hi = b - 1;
        while (lo < hi)
        {
            for (var c = 0; c < Ch; c++)
                (result[lo * Ch + c], result[hi * Ch + c]) = (result[hi * Ch + c], result[lo * Ch + c]);
            lo++; hi--;
        }
        return result;
    }

    /// <summary>Stereo-Breite über Mid/Side (amount: 0=mono, 1=original, &gt;1=breiter).</summary>
    public static float[] StereoWiden(float[] samples, int a, int b, double amount)
    {
        (a, b) = Clamp(samples, a, b);
        var result = (float[])samples.Clone();
        var s = (float)amount;
        for (var f = a; f < b; f++)
        {
            var l = result[f * Ch];
            var r = result[f * Ch + 1];
            var mid = (l + r) * 0.5f;
            var side = (l - r) * 0.5f * s;
            result[f * Ch] = mid + side;
            result[f * Ch + 1] = mid - side;
        }
        return result;
    }

    /// <summary>Echo/Delay mit Feedback (pro Kanal).</summary>
    public static float[] Echo(float[] samples, int a, int b, int sampleRate,
        double delayMs = 300, double feedback = 0.4, double mix = 0.5)
    {
        (a, b) = Clamp(samples, a, b);
        var result = (float[])samples.Clone();
        var delay = Math.Max(1, (int)(delayMs * 0.001 * sampleRate));

        for (var c = 0; c < Ch; c++)
        {
            var line = new float[delay];
            var pos = 0;
            for (var f = a; f < b; f++)
            {
                var idx = f * Ch + c;
                var dry = result[idx];
                var wet = line[pos];
                result[idx] = (float)(dry + mix * wet);
                line[pos] = (float)(dry + feedback * wet);
                pos = (pos + 1) % delay;
            }
        }
        return result;
    }

    /// <summary>Schroeder-Hall (4 Kamm- + 2 Allpassfilter, pro Kanal). mix = Wet-Anteil.</summary>
    public static float[] Reverb(float[] samples, int a, int b, int sampleRate, double mix = 0.3)
    {
        (a, b) = Clamp(samples, a, b);
        var result = (float[])samples.Clone();
        var scale = sampleRate / 44100.0;

        int[] combDelays = [1557, 1617, 1491, 1422];
        double[] combGains = [0.805, 0.827, 0.783, 0.764];
        int[] apDelays = [225, 556];
        const double apGain = 0.5;

        for (var c = 0; c < Ch; c++)
        {
            var combBufs = combDelays.Select(d => new float[Math.Max(1, (int)(d * scale))]).ToArray();
            var combPos = new int[combDelays.Length];
            var apBufs = apDelays.Select(d => new float[Math.Max(1, (int)(d * scale))]).ToArray();
            var apPos = new int[apDelays.Length];

            for (var f = a; f < b; f++)
            {
                var idx = f * Ch + c;
                var dry = result[idx];

                // Parallele Kammfilter.
                float combSum = 0;
                for (var k = 0; k < combBufs.Length; k++)
                {
                    var buf = combBufs[k];
                    var p = combPos[k];
                    var y = buf[p];
                    buf[p] = dry + (float)(combGains[k] * y);
                    combPos[k] = (p + 1) % buf.Length;
                    combSum += y;
                }
                combSum /= combBufs.Length;

                // Serielle Allpassfilter.
                var sig = combSum;
                for (var k = 0; k < apBufs.Length; k++)
                {
                    var buf = apBufs[k];
                    var p = apPos[k];
                    var bufVal = buf[p];
                    var outv = (float)(-apGain * sig + bufVal);
                    buf[p] = (float)(sig + apGain * outv);
                    apPos[k] = (p + 1) % buf.Length;
                    sig = outv;
                }

                result[idx] = (float)(dry * (1 - mix) + sig * mix);
            }
        }
        return result;
    }

    /// <summary>
    /// Vocal-Cleanup: glättet Gesangs-/Sprach-Spuren. Rumpel-Hochpass (75 Hz),
    /// leichtes Zähmen der Härte (~3 kHz), dezente „Luft", ein De-Esser gegen
    /// Zischlaute und eine sanfte, Stereo-gekoppelte Kompression für gleichmäßigere
    /// Lautstärke. <paramref name="strength"/> (0..1.5) skaliert die Intensität.
    /// </summary>
    public static float[] VocalCleanup(float[] samples, int a, int b, int sampleRate, double strength = 1.0)
    {
        (a, b) = Clamp(samples, a, b);
        var result = (float[])samples.Clone();
        var s = Math.Clamp(strength, 0, 1.5);

        const double deEssThresh = 0.06;            // Schwelle für Sibilanz-Erkennung
        var deEssAmount = 0.85 * s;                 // wie stark der Zisch-Anteil reduziert wird
        var attackCoef = Math.Exp(-1.0 / (0.001 * sampleRate));   // ~1 ms
        var releaseCoef = Math.Exp(-1.0 / (0.060 * sampleRate));  // ~60 ms

        for (var c = 0; c < Ch; c++)
        {
            var hp = Biquad.HighPass(sampleRate, 75, 0.707);                // Rumpeln raus
            var harsh = Biquad.Peaking(sampleRate, 3200, 1.2, -3.0 * s);    // Härte zähmen
            var air = Biquad.HighShelf(sampleRate, 11000, 0.707, 1.5 * s);  // dezente Luft
            var sibDetect = Biquad.HighPass(sampleRate, 6000, 0.707);       // Sibilanz-Detektor
            double env = 0;

            for (var f = a; f < b; f++)
            {
                var idx = f * Ch + c;
                var x = hp.Process(result[idx]);
                x = harsh.Process(x);
                x = air.Process(x);

                // De-Esser: Hüllkurve des Zisch-Bandes, dann nur diesen Anteil dämpfen.
                var sib = sibDetect.Process(x);
                var mag = Math.Abs(sib);
                var coef = mag > env ? attackCoef : releaseCoef;
                env = coef * env + (1 - coef) * mag;
                if (env > deEssThresh)
                {
                    var reduction = deEssThresh / env;   // < 1
                    x -= (float)(sib * (1 - reduction) * deEssAmount);
                }
                result[idx] = x;
            }
        }

        // Sanfte, Stereo-gekoppelte Kompression für gleichmäßigere Lautstärke.
        var comp = new Compressor(sampleRate, -22, 2.5, 8, 120, 2.0 * s);
        for (var f = a; f < b; f++)
        {
            var l = result[f * Ch];
            var r = result[f * Ch + 1];
            comp.Process(ref l, ref r);
            result[f * Ch] = l;
            result[f * Ch + 1] = r;
        }
        return result;
    }
}
