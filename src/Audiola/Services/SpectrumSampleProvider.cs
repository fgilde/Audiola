using NAudio.Wave;

namespace Audiola.Services;

/// <summary>
/// Greift den Audiostream ab (Mono-Summe) und hält die letzten Samples in einem Ringpuffer,
/// damit eine Echtzeit-Spektrum-Anzeige sie auslesen kann. Beeinflusst das Signal nicht.
/// </summary>
public sealed class SpectrumSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly int _channels;
    private readonly float[] _ring;
    private int _writePos;
    private readonly object _lock = new();

    public SpectrumSampleProvider(ISampleProvider source, int ringSize = 8192)
    {
        _source = source;
        _channels = Math.Max(1, source.WaveFormat.Channels);
        _ring = new float[ringSize];
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        var read = _source.Read(buffer, offset, count);
        lock (_lock)
        {
            for (var i = 0; i + _channels <= read; i += _channels)
            {
                float m = 0;
                for (var c = 0; c < _channels; c++) m += buffer[offset + i + c];
                _ring[_writePos] = m / _channels;
                _writePos = (_writePos + 1) % _ring.Length;
            }
        }
        return read;
    }

    /// <summary>Kopiert die letzten <c>dest.Length</c> Mono-Samples in Reihenfolge.</summary>
    public void CopyLatest(float[] dest)
    {
        lock (_lock)
        {
            var n = dest.Length;
            var start = _writePos - n;
            for (var i = 0; i < n; i++)
            {
                var idx = ((start + i) % _ring.Length + _ring.Length) % _ring.Length;
                dest[i] = _ring[idx];
            }
        }
    }
}
