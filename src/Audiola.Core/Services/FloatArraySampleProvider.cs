using NAudio.Wave;

namespace Audiola.Services;

/// <summary>Stellt ein interleaved Float-Array als <see cref="ISampleProvider"/> bereit.</summary>
public sealed class FloatArraySampleProvider : ISampleProvider
{
    private readonly float[] _samples;
    private int _position;

    public FloatArraySampleProvider(float[] interleaved, int sampleRate, int channels)
    {
        _samples = interleaved;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        var remaining = _samples.Length - _position;
        var n = Math.Min(remaining, count);
        if (n <= 0) return 0;
        Array.Copy(_samples, _position, buffer, offset, n);
        _position += n;
        return n;
    }
}
