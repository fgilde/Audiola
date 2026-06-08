namespace Audiola.Dsp;

/// <summary>Iterative Radix-2-FFT (Cooley-Tukey), in-place. Länge muss eine Zweierpotenz sein.</summary>
public static class Fft
{
    /// <summary>Vorwärts-FFT auf den komplexen Eingaben (re, im überschrieben).</summary>
    public static void Forward(float[] re, float[] im)
    {
        var n = re.Length;

        // Bit-Reversal-Permutation.
        for (int i = 1, j = 0; i < n; i++)
        {
            var bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j)
            {
                (re[i], re[j]) = (re[j], re[i]);
                (im[i], im[j]) = (im[j], im[i]);
            }
        }

        for (var len = 2; len <= n; len <<= 1)
        {
            var ang = -2.0 * Math.PI / len;
            float wlenRe = (float)Math.Cos(ang), wlenIm = (float)Math.Sin(ang);
            for (var i = 0; i < n; i += len)
            {
                float wRe = 1f, wIm = 0f;
                var half = len >> 1;
                for (var k = 0; k < half; k++)
                {
                    int a = i + k, b = i + k + half;
                    var vRe = re[b] * wRe - im[b] * wIm;
                    var vIm = re[b] * wIm + im[b] * wRe;
                    re[b] = re[a] - vRe; im[b] = im[a] - vIm;
                    re[a] += vRe; im[a] += vIm;
                    var nwRe = wRe * wlenRe - wIm * wlenIm;
                    wIm = wRe * wlenIm + wIm * wlenRe;
                    wRe = nwRe;
                }
            }
        }
    }
}
