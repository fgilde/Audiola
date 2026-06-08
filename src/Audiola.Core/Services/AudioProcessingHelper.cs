using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Audiola.Services;

public static class AudioProcessingHelper
{
    public static (float[] Samples, int SampleRate) ReadStereo(string filePath)
    {
        using var reader = new AudioFileReader(filePath);
        ISampleProvider provider = reader;
        if (provider.WaveFormat.Channels == 1)
            provider = new MonoToStereoSampleProvider(provider);

        var sampleRate = provider.WaveFormat.SampleRate;
        var buffer = new float[sampleRate * 2];
        var all = new List<float>(sampleRate * 2 * 4);

        int read;
        while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
            all.AddRange(buffer[..read]);

        return ([.. all], sampleRate);
    }

    public static double ApplyLoudnessTarget(float[] samples, int sampleRate, double targetLufs)
    {
        var measured = Audiola.Dsp.LoudnessMeter.MeasureIntegratedLufs(samples, sampleRate);
        if (double.IsInfinity(measured))
            return 0;

        var gainDb = targetLufs - measured;
        ApplyGain(samples, gainDb);
        return gainDb;
    }

    public static void ApplyGain(float[] samples, double gainDb)
    {
        var gain = (float)Math.Pow(10, gainDb / 20);
        for (var i = 0; i < samples.Length; i++)
            samples[i] *= gain;
    }

    public static int ApplyBrickwallClip(float[] samples)
    {
        var clipped = 0;
        for (var i = 0; i < samples.Length; i++)
        {
            if (samples[i] > 1f)
            {
                samples[i] = 1f;
                clipped++;
            }
            else if (samples[i] < -1f)
            {
                samples[i] = -1f;
                clipped++;
            }
        }

        return clipped;
    }

    public static float Lerp(float from, float to, double amount)
        => (float)(from + (to - from) * amount);

    /// <summary>RMS-Pegel einer Datei in dBFS (−∞ bei Stille). Für die Inhaltserkennung von Stems.</summary>
    public static double MeasureRmsDb(string path)
    {
        try
        {
            var (s, _) = ReadStereo(path);
            if (s.Length == 0) return double.NegativeInfinity;
            double sum = 0;
            for (var i = 0; i < s.Length; i++) sum += (double)s[i] * s[i];
            var rms = Math.Sqrt(sum / s.Length);
            return rms < 1e-9 ? double.NegativeInfinity : 20 * Math.Log10(rms);
        }
        catch { return double.NegativeInfinity; }
    }

    /// <summary>Resampelt interleaved Stereo von <paramref name="fromSr"/> auf <paramref name="toSr"/>.</summary>
    public static float[] Resample(float[] interleavedStereo, int fromSr, int toSr)
    {
        if (fromSr == toSr || interleavedStereo.Length == 0) return interleavedStereo;
        ISampleProvider src = new FloatArraySampleProvider(interleavedStereo, fromSr, 2);
        var rs = new WdlResamplingSampleProvider(src, toSr);
        var outList = new List<float>(interleavedStereo.Length);
        var buf = new float[toSr * 2];
        int read;
        while ((read = rs.Read(buf, 0, buf.Length)) > 0)
            outList.AddRange(buf[..read]);
        return [.. outList];
    }

    /// <summary>Ersetzt den Bereich [aS, bS) eines interleaved-Stereo-Puffers durch <paramref name="replacement"/>.</summary>
    public static float[] SpliceStereo(float[] seg, int aS, int bS, float[] replacement)
    {
        aS = Math.Clamp(aS, 0, seg.Length);
        bS = Math.Clamp(bS, aS, seg.Length);
        var head = aS;
        var tail = seg.Length - bS;
        var result = new float[head + replacement.Length + tail];
        Array.Copy(seg, 0, result, 0, head);
        Array.Copy(replacement, 0, result, head, replacement.Length);
        Array.Copy(seg, bS, result, head + replacement.Length, tail);
        return result;
    }
}
