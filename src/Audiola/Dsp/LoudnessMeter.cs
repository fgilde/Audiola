namespace Audiola.Dsp;

/// <summary>
/// Integrierte Lautheit nach ITU-R BS.1770 / EBU R128 (in LUFS).
///
/// K-Weighting (High-Shelf + High-Pass) wird per Abtastrate aus den
/// RBJ-Formeln neu berechnet, sodass die Messung bei 44.1 kHz ebenso korrekt
/// ist wie bei den 48-kHz-Referenzkoeffizienten aus BS.1770.
/// </summary>
public static class LoudnessMeter
{
    // K-Weighting-Parameter (aus pyloudnorm / BS.1770 abgeleitet).
    private const double ShelfFc = 1681.9744509555319;
    private const double ShelfQ = 0.7071752369554196;
    private const double ShelfGainDb = 3.999843853973347;
    private const double HpFc = 38.13547087602444;
    private const double HpQ = 0.5003270373238773;

    private const double AbsoluteGate = -70.0; // LUFS
    private const double RelativeGate = -10.0;  // LU

    /// <summary>
    /// Misst die integrierte Lautheit eines Stereo-Signals (interleaved L,R).
    /// Gibt <see cref="double.NegativeInfinity"/> zurueck, wenn das Signal zu leise/leer ist.
    /// </summary>
    public static double MeasureIntegratedLufs(float[] interleavedStereo, int sampleRate)
    {
        if (interleavedStereo.Length < 2) return double.NegativeInfinity;

        var frames = interleavedStereo.Length / 2;

        // K-Weighting pro Kanal.
        var shelfL = Biquad.HighShelf(sampleRate, ShelfFc, ShelfQ, ShelfGainDb);
        var hpL = Biquad.HighPass(sampleRate, HpFc, HpQ);
        var shelfR = Biquad.HighShelf(sampleRate, ShelfFc, ShelfQ, ShelfGainDb);
        var hpR = Biquad.HighPass(sampleRate, HpFc, HpQ);

        var wl = new double[frames];
        var wr = new double[frames];
        for (var i = 0; i < frames; i++)
        {
            wl[i] = hpL.Process(shelfL.Process(interleavedStereo[i * 2]));
            wr[i] = hpR.Process(shelfR.Process(interleavedStereo[i * 2 + 1]));
        }

        // 400-ms-Bloecke mit 100-ms-Schritt (75 % Ueberlappung).
        var blockSize = (int)(0.4 * sampleRate);
        var hop = (int)(0.1 * sampleRate);
        if (frames < blockSize) return double.NegativeInfinity;

        var blockLoudness = new List<double>();
        var blockMeanSquare = new List<double>();

        for (var start = 0; start + blockSize <= frames; start += hop)
        {
            double sumL = 0, sumR = 0;
            for (var i = start; i < start + blockSize; i++)
            {
                sumL += wl[i] * wl[i];
                sumR += wr[i] * wr[i];
            }
            var msL = sumL / blockSize;
            var msR = sumR / blockSize;
            var z = msL + msR; // Kanalgewichte L=R=1.0
            var loudness = -0.691 + 10 * Math.Log10(z + 1e-12);

            blockMeanSquare.Add(z);
            blockLoudness.Add(loudness);
        }

        // Absolutes Gate (-70 LUFS).
        double sumZ = 0;
        var count = 0;
        for (var i = 0; i < blockLoudness.Count; i++)
        {
            if (blockLoudness[i] >= AbsoluteGate)
            {
                sumZ += blockMeanSquare[i];
                count++;
            }
        }
        if (count == 0) return double.NegativeInfinity;

        var relativeThreshold = -0.691 + 10 * Math.Log10(sumZ / count) + RelativeGate;

        // Relatives Gate.
        sumZ = 0; count = 0;
        for (var i = 0; i < blockLoudness.Count; i++)
        {
            if (blockLoudness[i] >= AbsoluteGate && blockLoudness[i] >= relativeThreshold)
            {
                sumZ += blockMeanSquare[i];
                count++;
            }
        }
        if (count == 0) return double.NegativeInfinity;

        return -0.691 + 10 * Math.Log10(sumZ / count);
    }
}
