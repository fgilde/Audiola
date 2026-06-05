using NAudio.Wave;

namespace Audiola.Services;

/// <summary>Hängt den <see cref="LiveFxProcessor"/> in einen Sample-Stream ein.</summary>
internal sealed class LiveFxSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly LiveFxProcessor _processor;

    public LiveFxSampleProvider(ISampleProvider source, LiveFxProcessor processor)
    {
        _source = source;
        _processor = processor;
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        var read = _source.Read(buffer, offset, count);
        if (read > 0)
            _processor.Process(buffer, offset, read, WaveFormat.Channels);
        return read;
    }
}
