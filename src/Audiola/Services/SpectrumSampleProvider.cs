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
    private float _peakL, _peakR;   // Spitzenpegel seit dem letzten ReadPeaks() (fürs VU-Meter)
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

                // Stereo-Spitzenpegel fürs VU-Meter (Kanal 0 = L, Kanal 1 = R; Mono → beide gleich).
                var l = Math.Abs(buffer[offset + i]);
                var r = _channels > 1 ? Math.Abs(buffer[offset + i + 1]) : l;
                if (l > _peakL) _peakL = l;
                if (r > _peakR) _peakR = r;
            }
        }
        return read;
    }

    /// <summary>Liefert den Spitzenpegel L/R (0..1) seit dem letzten Aufruf und setzt ihn zurück.</summary>
    public (float L, float R) ReadPeaks()
    {
        lock (_lock)
        {
            var p = (_peakL, _peakR);
            _peakL = _peakR = 0f;
            return p;
        }
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
