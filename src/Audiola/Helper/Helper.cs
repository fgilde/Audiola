using NAudio.Dsp;
using NAudio.Wave;
using SoundTouch.Net.NAudioSupport;
using System.Buffers;

namespace Audiola.Helper;


public class NoiseAdder : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly float noiseLevel;
    private readonly Random rand = new Random();

    public WaveFormat WaveFormat => source.WaveFormat;

    public NoiseAdder(ISampleProvider source, float noiseLevel)
    {
        this.source = source;
        this.noiseLevel = Math.Clamp(noiseLevel, 0f, 0.05f); // Begrenzung
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = source.Read(buffer, offset, count);

        for (int i = 0; i < samplesRead; i++)
        {
            float noise = (float)(rand.NextDouble() * 2 - 1) * noiseLevel;
            buffer[offset + i] += noise;
        }
        return samplesRead;
    }
}

public sealed class StereoBufferSampleProvider : ISampleProvider, IWaveProvider
{
    private readonly float[] sourceBuffer;
    private int position;

    public StereoBufferSampleProvider(float[] interleavedStereo, int sampleRate)
    {
        if (interleavedStereo.Length % 2 != 0)
        {
            throw new ArgumentException("Der Puffer muss interleaved Stereo enthalten.", nameof(interleavedStereo));
        }

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "sampleRate muss positiv sein.");
        }

        sourceBuffer = interleavedStereo;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        var available = sourceBuffer.Length - position;
        if (available <= 0)
        {
            return 0;
        }

        var samplesToCopy = Math.Min(count, available);
        Array.Copy(sourceBuffer, position, buffer, offset, samplesToCopy);
        position += samplesToCopy;
        return samplesToCopy;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        var availableBytes = (sourceBuffer.Length - position) * sizeof(float);
        if (availableBytes <= 0)
        {
            return 0;
        }

        var bytesToCopy = Math.Min(count, availableBytes);
        var alignedBytes = bytesToCopy - (bytesToCopy % sizeof(float));
        if (alignedBytes <= 0)
        {
            return 0;
        }

        Buffer.BlockCopy(sourceBuffer, position * sizeof(float), buffer, offset, alignedBytes);
        position += alignedBytes / sizeof(float);
        return alignedBytes;
    }
}

public static class SampleProviderRenderer
{
    public static float[] RenderAllSamples(ISampleProvider provider, CancellationToken ct = default)
    {
        var chunkSize = Math.Max(provider.WaveFormat.SampleRate * provider.WaveFormat.Channels / 4, 4096);
        var chunk = new float[chunkSize];
        var writer = new ArrayBufferWriter<float>();

        int samplesRead;
        while ((samplesRead = provider.Read(chunk, 0, chunk.Length)) > 0)
        {
            ct.ThrowIfCancellationRequested();
            chunk.AsSpan(0, samplesRead).CopyTo(writer.GetSpan(samplesRead));
            writer.Advance(samplesRead);
        }

        return writer.WrittenSpan.ToArray();
    }
}

public sealed class FloatWaveProviderAdapter : IWaveProvider
{
    private readonly ISampleProvider source;
    private float[] readBuffer = [];

    public FloatWaveProviderAdapter(ISampleProvider source)
    {
        this.source = source;
    }

    public WaveFormat WaveFormat => source.WaveFormat;

    public int Read(byte[] buffer, int offset, int count)
    {
        var samplesRequired = count / sizeof(float);
        if (samplesRequired <= 0)
        {
            return 0;
        }

        if (readBuffer.Length < samplesRequired)
        {
            readBuffer = new float[samplesRequired];
        }

        var samplesRead = source.Read(readBuffer, 0, samplesRequired);
        var bytesRead = samplesRead * sizeof(float);
        Buffer.BlockCopy(readBuffer, 0, buffer, offset, bytesRead);
        return bytesRead;
    }
}

public class SoundTouchProfileProvider : ISampleProvider
{
    private readonly SoundTouchWaveProvider soundTouchProvider;

    public WaveFormat WaveFormat => soundTouchProvider.WaveFormat;

    public SoundTouchProfileProvider(ISampleProvider source, double pitchShiftSemitones = 0, double timeStretchFactor = 1.0)
    {
        soundTouchProvider = new SoundTouchWaveProvider(new FloatWaveProviderAdapter(source));
        soundTouchProvider.PitchSemiTones = pitchShiftSemitones;

        if (Math.Abs(timeStretchFactor - 1.0) > 0.001)
        {
            soundTouchProvider.TempoChange = ((1.0 / timeStretchFactor) - 1.0) * 100.0;
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var bytesRequested = count * sizeof(float);
        var byteBuffer = ArrayPool<byte>.Shared.Rent(bytesRequested);

        try
        {
            var bytesRead = soundTouchProvider.Read(byteBuffer, 0, bytesRequested);
            Buffer.BlockCopy(byteBuffer, 0, buffer, offset * sizeof(float), bytesRead);

            var samplesRead = bytesRead / sizeof(float);
            if (samplesRead < count)
            {
                Array.Clear(buffer, offset + samplesRead, count - samplesRead);
            }

            return samplesRead;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(byteBuffer);
        }
    }
}

public class BiQuadFilterProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly BiQuadFilter[]? lowCutFilters;
    private readonly BiQuadFilter[]? highCutFilters;

    public WaveFormat WaveFormat => source.WaveFormat;

    public BiQuadFilterProvider(ISampleProvider source, int lowCutHz = 0, int highCutHz = 0)
    {
        this.source = source;
        var channels = source.WaveFormat.Channels;

        if (lowCutHz > 0)
        {
            lowCutFilters = Enumerable
                .Range(0, channels)
                .Select(_ => BiQuadFilter.HighPassFilter(source.WaveFormat.SampleRate, lowCutHz, 1.0f))
                .ToArray();
        }

        if (highCutHz > 0 && highCutHz < source.WaveFormat.SampleRate / 2)
        {
            highCutFilters = Enumerable
                .Range(0, channels)
                .Select(_ => BiQuadFilter.LowPassFilter(source.WaveFormat.SampleRate, highCutHz, 1.0f))
                .ToArray();
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samples = source.Read(buffer, offset, count);

        for (int i = 0; i < samples; i++)
        {
            var channelIndex = i % source.WaveFormat.Channels;
            float sample = buffer[offset + i];

            if (lowCutFilters is not null)
                sample = lowCutFilters[channelIndex].Transform(sample);

            if (highCutFilters is not null)
                sample = highCutFilters[channelIndex].Transform(sample);

            buffer[offset + i] = sample;
        }
        return samples;
    }
}

public class ToneShaperProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly BiQuadFilter[]? lowShelfFilters;
    private readonly BiQuadFilter[]? highShelfFilters;

    public WaveFormat WaveFormat => source.WaveFormat;

    public ToneShaperProvider(ISampleProvider source, float lowShelfGainDb = 0, float highShelfGainDb = 0)
    {
        this.source = source;
        var channels = source.WaveFormat.Channels;

        if (Math.Abs(lowShelfGainDb) > 0.01f)
        {
            lowShelfFilters = Enumerable
                .Range(0, channels)
                .Select(_ => BiQuadFilter.LowShelf(source.WaveFormat.SampleRate, 180, 0.8f, lowShelfGainDb))
                .ToArray();
        }

        if (Math.Abs(highShelfGainDb) > 0.01f)
        {
            highShelfFilters = Enumerable
                .Range(0, channels)
                .Select(_ => BiQuadFilter.HighShelf(source.WaveFormat.SampleRate, 6500, 0.8f, highShelfGainDb))
                .ToArray();
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var samples = source.Read(buffer, offset, count);

        for (int i = 0; i < samples; i++)
        {
            var channelIndex = i % source.WaveFormat.Channels;
            var sample = buffer[offset + i];

            if (lowShelfFilters is not null)
            {
                sample = lowShelfFilters[channelIndex].Transform(sample);
            }

            if (highShelfFilters is not null)
            {
                sample = highShelfFilters[channelIndex].Transform(sample);
            }

            buffer[offset + i] = Math.Clamp(sample, -1f, 1f);
        }

        return samples;
    }
}

public class SaturationProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly float amount;

    public WaveFormat WaveFormat => source.WaveFormat;

    public SaturationProvider(ISampleProvider source, float amount)
    {
        this.source = source;
        this.amount = amount;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samples = source.Read(buffer, offset, count);

        for (int i = 0; i < samples; i++)
        {
            float s = buffer[offset + i];
            // Soft clipping
            buffer[offset + i] = (float)Math.Tanh(s * (1 + amount * 3)) / (1 + amount);
        }
        return samples;
    }
}

public class StereoWidenerProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly float width;

    public WaveFormat WaveFormat => source.WaveFormat;

    public StereoWidenerProvider(ISampleProvider source, float width)
    {
        this.source = source;
        this.width = Math.Max(0f, width);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var samples = source.Read(buffer, offset, count);
        var channels = source.WaveFormat.Channels;

        if (channels < 2)
        {
            return samples;
        }

        for (int i = 0; i + 1 < samples; i += channels)
        {
            var leftIndex = offset + i;
            var rightIndex = leftIndex + 1;
            var left = buffer[leftIndex];
            var right = buffer[rightIndex];

            var mid = (left + right) * 0.5f;
            var side = (left - right) * 0.5f * width;

            buffer[leftIndex] = Math.Clamp(mid + side, -1f, 1f);
            buffer[rightIndex] = Math.Clamp(mid - side, -1f, 1f);
        }

        return samples;
    }
}

public class PhaseFlipProvider : ISampleProvider
{
    private readonly ISampleProvider source;

    public WaveFormat WaveFormat => source.WaveFormat;

    public PhaseFlipProvider(ISampleProvider source)
    {
        this.source = source;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var samples = source.Read(buffer, offset, count);
        var channels = source.WaveFormat.Channels;

        if (channels < 2)
        {
            return samples;
        }

        for (int i = 1; i < samples; i += channels)
        {
            buffer[offset + i] = -buffer[offset + i];
        }

        return samples;
    }
}

public class BitCrusherProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly float quantizationLevels;

    public WaveFormat WaveFormat => source.WaveFormat;

    public BitCrusherProvider(ISampleProvider source, int bitDepth)
    {
        this.source = source;
        var clampedBitDepth = Math.Clamp(bitDepth, 4, 16);
        quantizationLevels = MathF.Pow(2f, clampedBitDepth - 1);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var samples = source.Read(buffer, offset, count);

        for (int i = 0; i < samples; i++)
        {
            var sample = buffer[offset + i];
            buffer[offset + i] = Math.Clamp(MathF.Round(sample * quantizationLevels) / quantizationLevels, -1f, 1f);
        }

        return samples;
    }
}

public class TremoloProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly float depth;
    private readonly double frequencyHz;
    private long samplePosition;

    public WaveFormat WaveFormat => source.WaveFormat;

    public TremoloProvider(ISampleProvider source, float depth, double frequencyHz)
    {
        this.source = source;
        this.depth = Math.Clamp(depth, 0f, 1f);
        this.frequencyHz = Math.Max(0.1, frequencyHz);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var samples = source.Read(buffer, offset, count);
        var channels = source.WaveFormat.Channels;

        for (int i = 0; i < samples; i++)
        {
            var frameIndex = samplePosition / channels;
            var lfo = (Math.Sin((2 * Math.PI * frequencyHz * frameIndex) / source.WaveFormat.SampleRate) + 1.0) * 0.5;
            var gain = 1f - (depth * (float)lfo);

            buffer[offset + i] *= gain;
            samplePosition++;
        }

        return samples;
    }
}

public class GainProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly float gainLinear;

    public WaveFormat WaveFormat => source.WaveFormat;

    public GainProvider(ISampleProvider source, float gainDb)
    {
        this.source = source;
        gainLinear = (float)Math.Pow(10.0, gainDb / 20.0);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var samples = source.Read(buffer, offset, count);

        for (int i = 0; i < samples; i++)
        {
            buffer[offset + i] = Math.Clamp(buffer[offset + i] * gainLinear, -1f, 1f);
        }

        return samples;
    }
}

public class SoftLimiterProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly float threshold;
    private readonly float kneeStrength;

    public WaveFormat WaveFormat => source.WaveFormat;

    public SoftLimiterProvider(ISampleProvider source, float threshold = 0.96f, float kneeStrength = 2.5f)
    {
        this.source = source;
        this.threshold = Math.Clamp(threshold, 0.7f, 0.999f);
        this.kneeStrength = Math.Max(1f, kneeStrength);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var samples = source.Read(buffer, offset, count);

        for (int i = 0; i < samples; i++)
        {
            var sample = buffer[offset + i];
            var magnitude = Math.Abs(sample);

            if (magnitude > threshold)
            {
                var excess = (magnitude - threshold) / (1f - threshold);
                var shaped = threshold + ((float)Math.Tanh(excess * kneeStrength) * (1f - threshold));
                buffer[offset + i] = Math.Sign(sample) * Math.Min(shaped, 0.999f);
            }
            else
            {
                buffer[offset + i] = sample;
            }
        }

        return samples;
    }
}

public class SimpleReverbProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly float wetness;
    private readonly float[] delayBuffer;
    private int delayBufferOffset;

    public WaveFormat WaveFormat => source.WaveFormat;

    public SimpleReverbProvider(ISampleProvider source, float wetness)
    {
        this.source = source;
        this.wetness = Math.Clamp(wetness, 0f, 0.4f);
        var delayFrames = Math.Max(1, source.WaveFormat.SampleRate / 6);
        delayBuffer = new float[delayFrames * source.WaveFormat.Channels];
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samples = source.Read(buffer, offset, count);

        for (int i = 0; i < samples; i++)
        {
            var input = buffer[offset + i];
            var delayed = delayBuffer[delayBufferOffset];
            var output = (input * (1f - wetness)) + (delayed * wetness);

            delayBuffer[delayBufferOffset] = Math.Clamp(input + (delayed * 0.35f), -1f, 1f);
            buffer[offset + i] = Math.Clamp(output, -1f, 1f);

            delayBufferOffset++;
            if (delayBufferOffset >= delayBuffer.Length)
            {
                delayBufferOffset = 0;
            }
        }

        return samples;
    }
}

public class SimpleCompressor : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly float thresholdDb = -12f;
    private readonly float ratio = 4f;

    public WaveFormat WaveFormat => source.WaveFormat;

    public SimpleCompressor(ISampleProvider source)
    {
        this.source = source;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samples = source.Read(buffer, offset, count);
        var thresholdLinear = (float)Math.Pow(10.0, thresholdDb / 20.0);

        for (int i = 0; i < samples; i++)
        {
            float sample = buffer[offset + i];
            var magnitude = Math.Abs(sample);

            if (magnitude > thresholdLinear)
            {
                var excess = magnitude - thresholdLinear;
                sample = Math.Sign(sample) * (thresholdLinear + (excess / ratio));
            }

            buffer[offset + i] = sample;
        }
        return samples;
    }
}