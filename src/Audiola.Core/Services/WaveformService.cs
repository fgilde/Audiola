using Audiola.Models;
using NAudio.Wave;

namespace Audiola.Services;

/// <summary>
/// Liest Audiodateien via NAudio und reduziert die Samples auf Min/Max-Peaks
/// pro Bucket. Unterstuetzt alle von NAudio dekodierbaren Formate (WAV, MP3, AIFF ...).
/// </summary>
public sealed class WaveformService : IWaveformService
{
    public Task<AudioTrack> LoadAsync(string filePath, int targetBuckets = 2000, CancellationToken ct = default)
        => Task.Run(() => Load(filePath, targetBuckets, ct), ct);

    private static AudioTrack Load(string filePath, int targetBuckets, CancellationToken ct)
    {
        using var reader = new AudioFileReader(filePath);

        var totalSamples = reader.Length / sizeof(float);
        var channels = reader.WaveFormat.Channels;
        var frames = totalSamples / Math.Max(channels, 1);
        var samplesPerBucket = (int)Math.Max(1, frames / targetBuckets);

        // Pro Bucket speichern wir Min und Max -> zwei Werte je Bucket.
        var peaks = new List<float>(targetBuckets * 2);

        var buffer = new float[reader.WaveFormat.SampleRate * channels];
        var frameIndexInBucket = 0;
        var bucketMin = float.MaxValue;
        var bucketMax = float.MinValue;

        int read;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            ct.ThrowIfCancellationRequested();

            for (var i = 0; i < read; i += channels)
            {
                // Auf Mono mischen.
                float sample = 0f;
                for (var c = 0; c < channels && i + c < read; c++)
                    sample += buffer[i + c];
                sample /= channels;

                if (sample < bucketMin) bucketMin = sample;
                if (sample > bucketMax) bucketMax = sample;

                if (++frameIndexInBucket >= samplesPerBucket)
                {
                    peaks.Add(bucketMin);
                    peaks.Add(bucketMax);
                    bucketMin = float.MaxValue;
                    bucketMax = float.MinValue;
                    frameIndexInBucket = 0;
                }
            }
        }

        if (frameIndexInBucket > 0 && bucketMax != float.MinValue)
        {
            peaks.Add(bucketMin);
            peaks.Add(bucketMax);
        }

        return new AudioTrack
        {
            FilePath = filePath,
            Duration = reader.TotalTime,
            SampleRate = reader.WaveFormat.SampleRate,
            Channels = channels,
            Peaks = [.. peaks]
        };
    }
}
