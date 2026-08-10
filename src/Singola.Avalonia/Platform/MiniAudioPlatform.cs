using System.Runtime.InteropServices;
using Audiola.Services.Audio;
using MiniAudioEx.Native;
using static MiniAudioEx.Native.MiniAudioNative;

namespace Singola.Avalonia.Platform;

/// <summary>
/// miniaudio-backed desktop I/O. The NuGet package supplies native assets for
/// Windows, macOS, and Linux, while miniaudio selects WASAPI, CoreAudio, or ALSA/PulseAudio.
/// </summary>
public sealed class MiniAudioPlatform : IAudioPlatform
{
    private readonly object _sync = new();
    private readonly ma_context_ptr _context = new(true);
    private readonly Dictionary<string, ma_device_id> _captureDeviceIds = new(StringComparer.Ordinal);
    private bool _disposed;

    public MiniAudioPlatform()
    {
        if (ma_context_init(null, _context) != ma_result.success)
        {
            _context.Free();
            throw new InvalidOperationException("The native audio context could not be initialized.");
        }
    }

    internal ma_context_ptr Context
    {
        get
        {
            ThrowIfDisposed();
            return _context;
        }
    }

    public IReadOnlyList<AudioInputDevice> GetInputDevices()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            if (ma_context_get_devices(_context, out _, out ma_device_info[] captureDevices) != ma_result.success)
                throw new InvalidOperationException("Microphone devices could not be enumerated.");

            _captureDeviceIds.Clear();
            var devices = new List<AudioInputDevice>();
            var availableDevices = captureDevices ?? [];

            for (var index = 0; index < availableDevices.Length; index++)
            {
                var id = $"capture:{index}";
                var captureDevice = availableDevices[index];
                _captureDeviceIds[id] = captureDevice.id;
                devices.Add(new AudioInputDevice(id, captureDevice.GetName(), captureDevice.isDefault > 0));
            }

            if (devices.Count == 0)
                devices.Add(new AudioInputDevice("default", "System default microphone", true));

            return devices;
        }
    }

    public IAudioPlayback CreatePlayback()
    {
        ThrowIfDisposed();
        return OperatingSystem.IsWindows()
            ? new WindowsNAudioPlayback()
            : new MiniAudioPlayback(Context);
    }

    public IAudioCapture CreateCapture(string? inputDeviceId = null)
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            ma_device_id? deviceId = null;
            if (!string.IsNullOrWhiteSpace(inputDeviceId) &&
                !string.Equals(inputDeviceId, "default", StringComparison.Ordinal))
            {
                if (!_captureDeviceIds.TryGetValue(inputDeviceId, out var selectedDevice))
                {
                    _ = GetInputDevices();
                    if (!_captureDeviceIds.TryGetValue(inputDeviceId, out selectedDevice))
                        throw new InvalidOperationException("The selected microphone is no longer available.");
                }

                deviceId = selectedDevice;
            }

            return new MiniAudioCapture(Context, deviceId);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            ma_context_uninit(_context);
            _context.Free();
            _captureDeviceIds.Clear();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

internal sealed class MiniAudioPlayback : IAudioPlayback
{
    private readonly object _sync = new();
    private readonly ma_context_ptr _context;
    private readonly ma_decoder_ptr _decoder = new(true);
    private readonly ma_device_ptr _device = new(true);
    private readonly ma_device_data_proc _dataCallback;
    private readonly IntPtr _framesRead = Marshal.AllocHGlobal(sizeof(ulong));
    private bool _decoderInitialized;
    private bool _deviceInitialized;
    private bool _opened;
    private bool _disposed;
    private int _isPlaying;
    private int _endRaised;
    private uint _channels;
    private uint _sampleRate;

    public MiniAudioPlayback(ma_context_ptr context)
    {
        _context = context;
        _dataCallback = OnData;
    }

    public event EventHandler? PlaybackEnded;

    public bool IsPlaying => Volatile.Read(ref _isPlaying) != 0;

    public TimeSpan Position
    {
        get
        {
            lock (_sync)
            {
                if (!_opened || ma_decoder_get_cursor_in_pcm_frames(_decoder, out var frames) != ma_result.success ||
                    _sampleRate == 0)
                    return TimeSpan.Zero;

                return TimeSpan.FromSeconds((double)frames / _sampleRate);
            }
        }
    }

    public TimeSpan Duration
    {
        get
        {
            lock (_sync)
            {
                if (!_opened || ma_decoder_get_length_in_pcm_frames(_decoder, out var frames) != ma_result.success ||
                    _sampleRate == 0)
                    return TimeSpan.Zero;

                return TimeSpan.FromSeconds((double)frames / _sampleRate);
            }
        }
    }

    public void Open(string path)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path)) throw new FileNotFoundException("Audio file was not found.", path);

        Close();
        lock (_sync)
        {
            var decoderConfig = ma_decoder_config_init(ma_format.f32, 0, 0);
            var result = OperatingSystem.IsWindows()
                ? ma_decoder_init_file_w(path, ref decoderConfig, _decoder)
                : ma_decoder_init_file(path, ref decoderConfig, _decoder);
            if (result != ma_result.success)
                throw new InvalidOperationException($"The audio file could not be decoded ({result}).");

            _decoderInitialized = true;
            unsafe
            {
                _channels = _decoder.Get()->outputChannels;
                _sampleRate = _decoder.Get()->outputSampleRate;
            }

            if (_channels == 0 || _sampleRate == 0)
            {
                CloseDecoder();
                throw new InvalidOperationException("The audio file does not expose a usable PCM format.");
            }

            var config = ma_device_config_init(ma_device_type.playback);
            config.playback.format = ma_format.f32;
            config.playback.channels = _channels;
            config.sampleRate = _sampleRate;
            config.SetDataCallback(_dataCallback);

            if (ma_device_init(_context, ref config, _device) != ma_result.success)
            {
                CloseDecoder();
                throw new InvalidOperationException("The default audio output device could not be opened.");
            }

            _deviceInitialized = true;
            _opened = true;
            Interlocked.Exchange(ref _endRaised, 0);
        }
    }

    public void Play()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            EnsureOpen();
            if (IsPlaying) return;
            Volatile.Write(ref _isPlaying, 1);
            if (ma_device_start(_device) != ma_result.success)
            {
                Volatile.Write(ref _isPlaying, 0);
                throw new InvalidOperationException("Audio playback could not be started.");
            }
        }
    }

    public void Pause()
    {
        ThrowIfDisposed();
        if (!_opened || !IsPlaying) return;
        Volatile.Write(ref _isPlaying, 0);
        if (ma_device_stop(_device) != ma_result.success)
        {
            Volatile.Write(ref _isPlaying, 1);
            throw new InvalidOperationException("Audio playback could not be paused.");
        }
    }

    public void Stop()
    {
        if (_disposed) return;
        Close();
    }

    public void Seek(TimeSpan position)
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            EnsureOpen();
            var frame = (ulong)(Math.Clamp(position.TotalSeconds, 0, Duration.TotalSeconds) * _sampleRate);
            if (ma_decoder_seek_to_pcm_frame(_decoder, frame) != ma_result.success)
                throw new InvalidOperationException("Audio playback could not seek to the requested position.");
            Interlocked.Exchange(ref _endRaised, 0);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Close();
        _decoder.Free();
        _device.Free();
        Marshal.FreeHGlobal(_framesRead);
    }

    private unsafe void OnData(ma_device_ptr device, IntPtr output, IntPtr input, uint frameCount)
    {
        if (!IsPlaying || output == IntPtr.Zero)
        {
            Clear(output, frameCount);
            return;
        }

        ulong readFrames = 0;
        lock (_sync)
        {
            if (_opened)
            {
                Marshal.WriteInt64(_framesRead, 0);
                _ = ma_decoder_read_pcm_frames(_decoder, output, frameCount, _framesRead);
                readFrames = unchecked((ulong)Marshal.ReadInt64(_framesRead));
            }
        }

        if (readFrames < frameCount)
        {
            Clear((IntPtr)((byte*)output + readFrames * _channels * sizeof(float)), frameCount - (uint)readFrames);
            if (Interlocked.Exchange(ref _endRaised, 1) == 0)
            {
                Volatile.Write(ref _isPlaying, 0);
                ThreadPool.QueueUserWorkItem(_ => PlaybackEnded?.Invoke(this, EventArgs.Empty));
            }
        }
    }

    private unsafe void Clear(IntPtr output, uint frames)
    {
        if (output == IntPtr.Zero || _channels == 0) return;
        new Span<float>((void*)output, checked((int)(frames * _channels))).Clear();
    }

    private void Close()
    {
        Volatile.Write(ref _isPlaying, 0);
        if (_deviceInitialized)
        {
            _ = ma_device_stop(_device);
            ma_device_uninit(_device);
            _deviceInitialized = false;
        }

        lock (_sync)
        {
            _opened = false;
            CloseDecoder();
            _channels = 0;
            _sampleRate = 0;
        }
    }

    private void CloseDecoder()
    {
        if (!_decoderInitialized) return;
        ma_decoder_uninit(_decoder);
        _decoderInitialized = false;
    }

    private void EnsureOpen()
    {
        if (!_opened) throw new InvalidOperationException("Open an audio file before controlling playback.");
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

internal sealed class MiniAudioCapture : IAudioCapture
{
    private const uint SampleRate = 44100;
    private readonly ma_context_ptr _context;
    private readonly ma_device_id? _deviceId;
    private readonly ma_device_ptr _device = new(true);
    private readonly ma_device_data_proc _dataCallback;
    private bool _deviceInitialized;
    private bool _disposed;
    private int _isRecording;

    public MiniAudioCapture(ma_context_ptr context, ma_device_id? deviceId)
    {
        _context = context;
        _deviceId = deviceId;
        _dataCallback = OnData;
    }

    public event EventHandler<AudioSamplesEventArgs>? SamplesAvailable;

    public bool IsRecording => Volatile.Read(ref _isRecording) != 0;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsRecording) return;

        var config = ma_device_config_init(ma_device_type.capture);
        config.capture.format = ma_format.f32;
        config.capture.channels = 1;
        config.sampleRate = SampleRate;
        config.SetDataCallback(_dataCallback);

        if (_deviceId is { } selectedDevice)
        {
            config.capture.pDeviceID = new ma_device_id_ptr(true);
            unsafe
            {
                *config.capture.pDeviceID.Get() = selectedDevice;
            }
        }

        try
        {
            if (ma_device_init(_context, ref config, _device) != ma_result.success)
                throw new InvalidOperationException("The microphone could not be opened.");
            _deviceInitialized = true;

            if (ma_device_start(_device) != ma_result.success)
                throw new InvalidOperationException("The microphone could not be started.");
            Volatile.Write(ref _isRecording, 1);
        }
        finally
        {
            if (config.capture.pDeviceID.pointer != IntPtr.Zero)
                config.capture.pDeviceID.Free();
        }
    }

    public void Stop()
    {
        Volatile.Write(ref _isRecording, 0);
        if (!_deviceInitialized) return;
        _ = ma_device_stop(_device);
        ma_device_uninit(_device);
        _deviceInitialized = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _device.Free();
    }

    private void OnData(ma_device_ptr device, IntPtr output, IntPtr input, uint frameCount)
    {
        if (!IsRecording || input == IntPtr.Zero || frameCount == 0) return;

        var samples = new float[frameCount];
        Marshal.Copy(input, samples, 0, samples.Length);
        SamplesAvailable?.Invoke(this, new AudioSamplesEventArgs(samples, (int)SampleRate, 1));
    }
}
