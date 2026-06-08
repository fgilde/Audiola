using NAudio.Wave;

namespace Audiola.Services;

/// <summary>
/// Schnitt-Operationen auf einem interleaved Stereo-Float-Puffer (2 Kanäle).
/// „Frame“ = ein L/R-Sample-Paar. Alle Methoden sind rein und gut testbar.
/// </summary>
public static class AudioEdits
{
    public const int Channels = 2;

    public static int FrameCount(float[] samples) => samples.Length / Channels;

    /// <summary>Entfernt den Bereich [startFrame, endFrame) und fügt den Rest zusammen.</summary>
    public static float[] Delete(float[] samples, int startFrame, int endFrame)
    {
        (startFrame, endFrame) = Clamp(samples, startFrame, endFrame);
        var headLen = startFrame * Channels;
        var tailStart = endFrame * Channels;
        var tailLen = samples.Length - tailStart;

        var result = new float[headLen + tailLen];
        Array.Copy(samples, 0, result, 0, headLen);
        Array.Copy(samples, tailStart, result, headLen, tailLen);
        return result;
    }

    /// <summary>Behält nur den Bereich [startFrame, endFrame).</summary>
    public static float[] Trim(float[] samples, int startFrame, int endFrame)
    {
        (startFrame, endFrame) = Clamp(samples, startFrame, endFrame);
        var len = (endFrame - startFrame) * Channels;
        var result = new float[len];
        Array.Copy(samples, startFrame * Channels, result, 0, len);
        return result;
    }

    /// <summary>Setzt den Bereich [startFrame, endFrame) auf Stille (in-place auf einer Kopie).</summary>
    public static float[] Silence(float[] samples, int startFrame, int endFrame)
    {
        (startFrame, endFrame) = Clamp(samples, startFrame, endFrame);
        var result = (float[])samples.Clone();
        Array.Clear(result, startFrame * Channels, (endFrame - startFrame) * Channels);
        return result;
    }

    /// <summary>Linearer Fade über den Bereich (in=0→1, out=1→0).</summary>
    public static float[] Fade(float[] samples, int startFrame, int endFrame, bool fadeIn)
    {
        (startFrame, endFrame) = Clamp(samples, startFrame, endFrame);
        var result = (float[])samples.Clone();
        var n = endFrame - startFrame;
        if (n <= 0) return result;

        for (var f = 0; f < n; f++)
        {
            var t = (double)f / n;            // 0..1
            var gain = (float)(fadeIn ? t : 1 - t);
            var idx = (startFrame + f) * Channels;
            result[idx] *= gain;
            result[idx + 1] *= gain;
        }
        return result;
    }

    private static (int, int) Clamp(float[] samples, int start, int end)
    {
        var frames = FrameCount(samples);
        start = Math.Clamp(start, 0, frames);
        end = Math.Clamp(end, 0, frames);
        if (end < start) (start, end) = (end, start);
        return (start, end);
    }

    /// <summary>
    /// Min/Max-Peaks pro Bucket (mono gemischt), gleiches Format wie der WaveformService:
    /// zwei Werte je Bucket.
    /// </summary>
    public static float[] ComputePeaks(float[] samples, int targetBuckets = 2000)
    {
        var frames = FrameCount(samples);
        if (frames == 0) return [];

        var perBucket = Math.Max(1, frames / targetBuckets);
        var peaks = new List<float>(targetBuckets * 2);

        var min = float.MaxValue;
        var max = float.MinValue;
        var count = 0;

        for (var f = 0; f < frames; f++)
        {
            var mono = (samples[f * Channels] + samples[f * Channels + 1]) * 0.5f;
            if (mono < min) min = mono;
            if (mono > max) max = mono;

            if (++count >= perBucket)
            {
                peaks.Add(min);
                peaks.Add(max);
                min = float.MaxValue;
                max = float.MinValue;
                count = 0;
            }
        }
        if (count > 0 && max != float.MinValue)
        {
            peaks.Add(min);
            peaks.Add(max);
        }
        return [.. peaks];
    }

    /// <summary>Schreibt den Puffer als 32-bit-Float-WAV (verlustfrei für Edit-Round-Trips).</summary>
    public static void WriteWav(string path, float[] samples, int sampleRate)
    {
        using var writer = new WaveFileWriter(path, WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, Channels));
        writer.WriteSamples(samples, 0, samples.Length);
    }
}
