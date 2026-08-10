using Audiola.ViewModels;
using NAudio.Wave;

namespace Audiola.Services;

/// <summary>
/// Spielt einen Ausschnitt einer Quelle (srcStart … srcStart+len) ab einem
/// Timeline-Offset (Stille davor). Damit lässt sich eine Quelle in mehrere
/// unabhängige Clips auf einer Spur aufteilen. Unterstützt Seeken über die
/// Ausgabe-Framenummer (Timeline-Zeit).
/// </summary>
public sealed class ClipSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly AudioFileReader _reader;
    private readonly ClipViewModel? _clip;
    private readonly int _channels;
    private readonly int _sampleRate;
    private readonly long _offsetSamples;    // interleaved Stille davor
    private readonly long _srcStartSamples;  // interleaved Startversatz in der Quelle
    private readonly long _srcLenSamples;    // interleaved Länge des Ausschnitts

    private long _silenceRemaining;
    private long _srcRemaining;

    public ClipSampleProvider(ISampleProvider source, AudioFileReader reader,
        long offsetSamples, long srcStartSamples, long srcLenSamples, ClipViewModel? clip = null)
    {
        _source = source;
        _reader = reader;
        _clip = clip;
        _channels = source.WaveFormat.Channels;
        _sampleRate = source.WaveFormat.SampleRate;
        _offsetSamples = Math.Max(0, offsetSamples);
        _srcStartSamples = Math.Max(0, srcStartSamples);
        _srcLenSamples = Math.Max(0, srcLenSamples);

        _silenceRemaining = _offsetSamples;
        _srcRemaining = _srcLenSamples;
        SeekSource(_srcStartSamples);
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        var produced = 0;

        if (_silenceRemaining > 0)
        {
            var sil = (int)Math.Min(_silenceRemaining, count);
            Array.Clear(buffer, offset, sil);
            _silenceRemaining -= sil;
            produced += sil;
        }

        if (produced < count && _srcRemaining > 0)
        {
            var toRead = (int)Math.Min(count - produced, _srcRemaining);
            var read = _source.Read(buffer, offset + produced, toRead);
            ApplyGainAndFades(buffer, offset + produced, read, _srcLenSamples - _srcRemaining);
            _srcRemaining -= read;
            produced += read;
        }

        // Nach Clip-Ende mit Stille auffüllen (immer count zurückgeben). Sonst entfernt der
        // MixingSampleProvider diesen Eingang dauerhaft, und der Clip wäre nach Seek/Neustart stumm.
        if (produced < count)
        {
            Array.Clear(buffer, offset + produced, count - produced);
            produced = count;
        }

        return produced;
    }

    /// <summary>Wendet Clip-Gain und Ein-/Ausblendung live an (interleaved-Positionen).</summary>
    private void ApplyGainAndFades(float[] buffer, int start, int count, long audioPosStart)
    {
        if (_clip is null) return;

        var gain = (float)Math.Pow(10, _clip.GainDb / 20);
        var fadeIn = (long)(Math.Max(0, _clip.FadeInSeconds) * _sampleRate) * _channels;
        var fadeOut = (long)(Math.Max(0, _clip.FadeOutSeconds) * _sampleRate) * _channels;

        for (var j = 0; j < count; j++)
        {
            var pos = audioPosStart + j;
            var env = gain;
            if (fadeIn > 0 && pos < fadeIn)
                env *= (float)(pos / (double)fadeIn);
            var remaining = _srcLenSamples - pos;
            if (fadeOut > 0 && remaining < fadeOut)
                env *= (float)(Math.Max(0, remaining) / (double)fadeOut);
            buffer[start + j] *= env;
        }
    }

    public void SeekToOutputSeconds(double seconds)
    {
        var outSamples = (long)(Math.Max(0, seconds) * _sampleRate) * _channels;
        if (outSamples < _offsetSamples)
        {
            _silenceRemaining = _offsetSamples - outSamples;
            _srcRemaining = _srcLenSamples;
            SeekSource(_srcStartSamples);
        }
        else
        {
            _silenceRemaining = 0;
            var into = outSamples - _offsetSamples;
            if (into >= _srcLenSamples)
            {
                _srcRemaining = 0;
            }
            else
            {
                _srcRemaining = _srcLenSamples - into;
                SeekSource(_srcStartSamples + into);
            }
        }
    }

    private void SeekSource(long interleavedSample)
    {
        // Sample-genau über die Byte-Position (zeitbasiertes Seeken rundet ungenau).
        var frames = interleavedSample / _channels;
        var pos = frames * _reader.WaveFormat.BlockAlign;
        _reader.Position = Math.Clamp(pos, 0, _reader.Length);
    }
}

/// <summary>Zählt die gelesenen Samples, um die Timeline-Position zu bestimmen.</summary>
public sealed class CountingSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;

    public CountingSampleProvider(ISampleProvider source) => _source = source;

    public long SamplesRead { get; private set; }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        var read = _source.Read(buffer, offset, count);
        SamplesRead += read;
        return read;
    }

    public void SetSamples(long samples) => SamplesRead = Math.Max(0, samples);
}
