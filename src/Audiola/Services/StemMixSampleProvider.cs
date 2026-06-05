using NAudio.Wave;

namespace Audiola.Services;

/// <summary>
/// Wendet Lautstaerke und (Constant-Power-)Panorama auf einen Stereo-Sample-Stream an.
/// </summary>
internal sealed class StemMixSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float _leftGain;
    private readonly float _rightGain;

    public StemMixSampleProvider(ISampleProvider source, float volume, float pan)
    {
        if (source.WaveFormat.Channels != 2)
            throw new ArgumentException("Quelle muss Stereo sein.", nameof(source));

        _source = source;

        // Constant-Power-Panning.
        var angle = (pan + 1f) * 0.25f * MathF.PI; // 0..pi/2
        _leftGain = volume * MathF.Cos(angle);
        _rightGain = volume * MathF.Sin(angle);
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        var read = _source.Read(buffer, offset, count);
        for (var i = 0; i < read; i += 2)
        {
            buffer[offset + i] *= _leftGain;
            if (i + 1 < read)
                buffer[offset + i + 1] *= _rightGain;
        }
        return read;
    }
}
