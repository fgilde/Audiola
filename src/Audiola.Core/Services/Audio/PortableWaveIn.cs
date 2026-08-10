using System.Runtime.InteropServices;
using MiniAudioEx.Native;
using NAudio.Wave;
using static MiniAudioEx.Native.MiniAudioNative;

namespace Audiola.Services.Audio;

/// <summary>
/// Mikrofon-Aufnahme für alle Plattformen. Unter Windows bleibt es bei NAudios
/// <see cref="WaveInEvent"/>; auf macOS und Linux übernimmt miniaudio.
///
/// Die Signatur entspricht <see cref="WaveInEvent"/> (DeviceNumber, WaveFormat,
/// BufferMilliseconds, DataAvailable mit 16-Bit-PCM), damit Aufnahme, Einsing-Studio
/// und Latenz-Messung unverändert weiterlaufen.
/// </summary>
public sealed class PortableWaveIn : IWaveIn
{
    private readonly IWaveIn _inner;

    public PortableWaveIn()
        => _inner = OperatingSystem.IsWindows() ? new WaveInEvent() : new MiniAudioWaveIn();

    /// <summary>Index des Eingabegeräts in <see cref="AudioDevices.InputNames"/>.</summary>
    public int DeviceNumber
    {
        get => _inner switch
        {
            WaveInEvent w => w.DeviceNumber,
            MiniAudioWaveIn m => m.DeviceNumber,
            _ => 0
        };
        set
        {
            switch (_inner)
            {
                case WaveInEvent w: w.DeviceNumber = value; break;
                case MiniAudioWaveIn m: m.DeviceNumber = value; break;
            }
        }
    }

    /// <summary>Puffergröße in Millisekunden (bestimmt die Aufnahme-Latenz).</summary>
    public int BufferMilliseconds
    {
        get => _inner switch
        {
            WaveInEvent w => w.BufferMilliseconds,
            MiniAudioWaveIn m => m.BufferMilliseconds,
            _ => 100
        };
        set
        {
            switch (_inner)
            {
                case WaveInEvent w: w.BufferMilliseconds = value; break;
                case MiniAudioWaveIn m: m.BufferMilliseconds = value; break;
            }
        }
    }

    public WaveFormat WaveFormat
    {
        get => _inner.WaveFormat;
        set => _inner.WaveFormat = value;
    }

    public event EventHandler<WaveInEventArgs>? DataAvailable
    {
        add => _inner.DataAvailable += value;
        remove => _inner.DataAvailable -= value;
    }

    public event EventHandler<StoppedEventArgs>? RecordingStopped
    {
        add => _inner.RecordingStopped += value;
        remove => _inner.RecordingStopped -= value;
    }

    public void StartRecording() => _inner.StartRecording();

    public void StopRecording() => _inner.StopRecording();

    public void Dispose() => _inner.Dispose();
}

/// <summary>
/// miniaudio-Aufnahme, die 16-Bit-PCM in <see cref="DataAvailable"/> liefert — genau das
/// Format, das die vorhandenen Auswerter (Pegel, Tonhöhe, WAV-Writer) erwarten.
/// </summary>
internal sealed class MiniAudioWaveIn : IWaveIn
{
    private readonly object _sync = new();
    private readonly ma_context_ptr _context = new(true);
    private readonly ma_device_ptr _device = new(true);
    private readonly ma_device_data_proc _dataCallback;

    private byte[] _buffer = [];
    private bool _contextInitialized;
    private bool _deviceInitialized;
    private bool _recording;
    private bool _disposed;

    public MiniAudioWaveIn() => _dataCallback = OnData;

    public int DeviceNumber { get; set; }

    public int BufferMilliseconds { get; set; } = 100;

    public WaveFormat WaveFormat { get; set; } = new(44100, 16, 1);

    public event EventHandler<WaveInEventArgs>? DataAvailable;

    public event EventHandler<StoppedEventArgs>? RecordingStopped;

    public void StartRecording()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            if (_recording) return;

            if (!_contextInitialized)
            {
                if (ma_context_init(null, _context) != ma_result.success)
                    throw new InvalidOperationException("Der Audio-Kontext konnte nicht initialisiert werden.");
                _contextInitialized = true;
            }

            var config = ma_device_config_init(ma_device_type.capture);
            config.capture.format = ma_format.s16;                       // direkt im Zielformat aufnehmen
            config.capture.channels = (uint)WaveFormat.Channels;
            config.sampleRate = (uint)WaveFormat.SampleRate;
            config.periodSizeInMilliseconds = (uint)Math.Max(10, BufferMilliseconds);
            config.SetDataCallback(_dataCallback);

            var deviceId = ResolveDeviceId();
            try
            {
                if (deviceId is not null)
                {
                    config.capture.pDeviceID = new ma_device_id_ptr(true);
                    unsafe { *config.capture.pDeviceID.Get() = deviceId.Value; }
                }

                if (ma_device_init(_context, ref config, _device) != ma_result.success)
                    throw new InvalidOperationException("Das Mikrofon konnte nicht geöffnet werden.");
                _deviceInitialized = true;

                if (ma_device_start(_device) != ma_result.success)
                {
                    ma_device_uninit(_device);
                    _deviceInitialized = false;
                    throw new InvalidOperationException("Die Aufnahme konnte nicht gestartet werden.");
                }
                _recording = true;
            }
            finally
            {
                if (config.capture.pDeviceID.pointer != IntPtr.Zero) config.capture.pDeviceID.Free();
            }
        }
    }

    public void StopRecording()
    {
        lock (_sync)
        {
            if (_disposed || !_recording) return;
            ma_device_stop(_device);
            ma_device_uninit(_device);
            _deviceInitialized = false;
            _recording = false;
        }
        RecordingStopped?.Invoke(this, new StoppedEventArgs());
    }

    /// <summary>Wählt das Gerät nach Index; <c>null</c> = Standardgerät des Systems.</summary>
    private ma_device_id? ResolveDeviceId()
    {
        if (DeviceNumber <= 0) return null;
        if (ma_context_get_devices(_context, out _, out ma_device_info[] captureDevices) != ma_result.success)
            return null;
        var devices = captureDevices ?? [];
        return DeviceNumber < devices.Length ? devices[DeviceNumber].id : null;
    }

    private unsafe void OnData(ma_device_ptr device, IntPtr output, IntPtr input, uint frameCount)
    {
        var handler = DataAvailable;
        if (handler is null || input == IntPtr.Zero || frameCount == 0) return;

        var bytes = (int)frameCount * WaveFormat.Channels * 2;   // s16
        if (_buffer.Length < bytes) _buffer = new byte[bytes];
        new Span<byte>((void*)input, bytes).CopyTo(_buffer);

        try { handler(this, new WaveInEventArgs(_buffer, bytes)); }
        catch { /* ein fehlerhafter Abnehmer darf die Aufnahme nicht abbrechen */ }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            if (_recording) { ma_device_stop(_device); _recording = false; }
            if (_deviceInitialized) ma_device_uninit(_device);
            _device.Free();
            if (_contextInitialized) ma_context_uninit(_context);
            _context.Free();
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}

/// <summary>Eingabegeräte des Systems — plattformneutral (ersetzt <c>WaveInEvent.DeviceCount</c>).</summary>
public static class AudioDevices
{
    /// <summary>Namen aller Aufnahme-Geräte; Index = <see cref="PortableWaveIn.DeviceNumber"/>.</summary>
    public static IReadOnlyList<string> InputNames
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                var names = new List<string>();
                for (var i = 0; i < WaveInEvent.DeviceCount; i++)
                {
                    try { names.Add(WaveInEvent.GetCapabilities(i).ProductName); }
                    catch { names.Add($"Mikrofon {i + 1}"); }
                }
                return names;
            }

            var context = new ma_context_ptr(true);
            try
            {
                if (ma_context_init(null, context) != ma_result.success) return [];
                if (ma_context_get_devices(context, out _, out ma_device_info[] captureDevices) != ma_result.success)
                    return [];
                var devices = captureDevices ?? [];
                return devices.Length == 0
                    ? ["Standard-Mikrofon"]
                    : [.. devices.Select(d => d.GetName())];
            }
            catch { return []; }
            finally
            {
                try { ma_context_uninit(context); } catch { }
                context.Free();
            }
        }
    }
}
