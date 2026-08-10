using System.Runtime.InteropServices;
using MiniAudioEx.Native;
using NAudio.Wave;
using static MiniAudioEx.Native.MiniAudioNative;

namespace Audiola.Services.Audio;

/// <summary>
/// Audio-Ausgabe für alle Plattformen. Unter Windows bleibt es bei NAudios
/// <see cref="WaveOutEvent"/> (bewährtes Verhalten, keine Änderung am Bestand);
/// auf macOS und Linux übernimmt miniaudio (CoreAudio bzw. ALSA/PulseAudio).
///
/// Die Signatur entspricht <see cref="WaveOutEvent"/>, damit die vorhandenen
/// SampleProvider-Ketten (Live-EQ, FX, Master, Stem-Mix) unverändert weiterlaufen.
/// </summary>
public sealed class PortableWaveOut : IWavePlayer
{
    private readonly IWavePlayer _inner;

    public PortableWaveOut()
        => _inner = OperatingSystem.IsWindows() ? new WaveOutEvent() : new MiniAudioWaveOut();

    /// <summary>Gewünschte Latenz in Millisekunden (wird an das jeweilige Backend gemeldet).</summary>
    public int DesiredLatency
    {
        get => _inner switch
        {
            WaveOutEvent w => w.DesiredLatency,
            MiniAudioWaveOut m => m.DesiredLatency,
            _ => 200
        };
        set
        {
            switch (_inner)
            {
                case WaveOutEvent w: w.DesiredLatency = value; break;
                case MiniAudioWaveOut m: m.DesiredLatency = value; break;
            }
        }
    }

    public PlaybackState PlaybackState => _inner.PlaybackState;

    public float Volume
    {
        get => _inner.Volume;
        set => _inner.Volume = value;
    }

    public WaveFormat OutputWaveFormat => _inner.OutputWaveFormat;

    public event EventHandler<StoppedEventArgs>? PlaybackStopped
    {
        add => _inner.PlaybackStopped += value;
        remove => _inner.PlaybackStopped -= value;
    }

    public void Init(IWaveProvider waveProvider) => _inner.Init(waveProvider);

    public void Play() => _inner.Play();

    public void Pause() => _inner.Pause();

    public void Stop() => _inner.Stop();

    public void Dispose() => _inner.Dispose();
}

/// <summary>
/// miniaudio-Ausgabe, die einen NAudio-<see cref="IWaveProvider"/> im Geräte-Callback leert.
/// Erwartet 32-Bit-Float-Samples (die Ketten der App liefern genau das); 16-Bit-PCM wird
/// beim Lesen umgerechnet.
/// </summary>
internal sealed class MiniAudioWaveOut : IWavePlayer
{
    private readonly object _sync = new();
    private readonly ma_context_ptr _context = new(true);
    private readonly ma_device_ptr _device = new(true);
    private readonly ma_device_data_proc _dataCallback;

    private IWaveProvider? _source;
    private byte[] _readBuffer = [];
    private bool _contextInitialized;
    private bool _deviceInitialized;
    private bool _disposed;
    private float _volume = 1f;
    private volatile PlaybackState _state = PlaybackState.Stopped;

    public MiniAudioWaveOut() => _dataCallback = OnData;

    public int DesiredLatency { get; set; } = 200;

    public PlaybackState PlaybackState => _state;

    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0f, 1f);
    }

    public WaveFormat OutputWaveFormat => _source?.WaveFormat ?? new WaveFormat(44100, 2);

    public event EventHandler<StoppedEventArgs>? PlaybackStopped;

    public void Init(IWaveProvider waveProvider)
    {
        ArgumentNullException.ThrowIfNull(waveProvider);
        lock (_sync)
        {
            ThrowIfDisposed();
            TearDownDevice();
            _source = waveProvider;

            if (!_contextInitialized)
            {
                if (ma_context_init(null, _context) != ma_result.success)
                    throw new InvalidOperationException("Der Audio-Kontext konnte nicht initialisiert werden.");
                _contextInitialized = true;
            }

            var format = waveProvider.WaveFormat;
            var config = ma_device_config_init(ma_device_type.playback);
            config.playback.format = format.Encoding == WaveFormatEncoding.IeeeFloat
                ? ma_format.f32
                : ma_format.s16;
            config.playback.channels = (uint)format.Channels;
            config.sampleRate = (uint)format.SampleRate;
            config.SetDataCallback(_dataCallback);

            if (ma_device_init(_context, ref config, _device) != ma_result.success)
                throw new InvalidOperationException("Das Standard-Ausgabegerät konnte nicht geöffnet werden.");

            _deviceInitialized = true;
        }
    }

    public void Play()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            if (!_deviceInitialized) throw new InvalidOperationException("Init muss vor Play aufgerufen werden.");
            if (_state == PlaybackState.Playing) return;
            if (ma_device_start(_device) != ma_result.success)
                throw new InvalidOperationException("Die Wiedergabe konnte nicht gestartet werden.");
            _state = PlaybackState.Playing;
        }
    }

    public void Pause()
    {
        lock (_sync)
        {
            if (_disposed || _state != PlaybackState.Playing) return;
            ma_device_stop(_device);
            _state = PlaybackState.Paused;
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            if (_disposed || _state == PlaybackState.Stopped) return;
            if (_deviceInitialized) ma_device_stop(_device);
            _state = PlaybackState.Stopped;
        }
        PlaybackStopped?.Invoke(this, new StoppedEventArgs());
    }

    private unsafe void OnData(ma_device_ptr device, IntPtr output, IntPtr input, uint frameCount)
    {
        var source = _source;
        if (output == IntPtr.Zero) return;

        var format = source?.WaveFormat;
        var bytesPerFrame = format?.BlockAlign ?? 8;
        var wanted = (int)frameCount * bytesPerFrame;

        if (source is null || _state != PlaybackState.Playing)
        {
            new Span<byte>((void*)output, wanted).Clear();
            return;
        }

        if (_readBuffer.Length < wanted) _readBuffer = new byte[wanted];

        int read;
        try { read = source.Read(_readBuffer, 0, wanted); }
        catch { read = 0; }

        var target = new Span<byte>((void*)output, wanted);
        if (read > 0)
        {
            _readBuffer.AsSpan(0, read).CopyTo(target);
            ApplyVolume(target[..read], format!);
        }
        if (read < wanted) target[read..].Clear();

        // Quelle erschöpft: wie WaveOutEvent das Ende melden (die Engines hängen daran).
        if (read == 0) RaiseEndOnce();
    }

    private void ApplyVolume(Span<byte> pcm, WaveFormat format)
    {
        var volume = _volume;
        if (Math.Abs(volume - 1f) < 0.001f) return;

        if (format.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            var floats = MemoryMarshal.Cast<byte, float>(pcm);
            for (var i = 0; i < floats.Length; i++) floats[i] *= volume;
        }
        else
        {
            var shorts = MemoryMarshal.Cast<byte, short>(pcm);
            for (var i = 0; i < shorts.Length; i++)
                shorts[i] = (short)Math.Clamp(shorts[i] * volume, short.MinValue, short.MaxValue);
        }
    }

    private int _endRaised;

    private void RaiseEndOnce()
    {
        if (Interlocked.Exchange(ref _endRaised, 1) != 0) return;
        _state = PlaybackState.Stopped;
        PlaybackStopped?.Invoke(this, new StoppedEventArgs());
    }

    private void TearDownDevice()
    {
        if (!_deviceInitialized) return;
        ma_device_stop(_device);
        ma_device_uninit(_device);
        _deviceInitialized = false;
        _state = PlaybackState.Stopped;
        Interlocked.Exchange(ref _endRaised, 0);
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            TearDownDevice();
            _device.Free();
            if (_contextInitialized) ma_context_uninit(_context);
            _context.Free();
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
