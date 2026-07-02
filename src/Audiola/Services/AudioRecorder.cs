using System.IO;
using NAudio.Wave;

namespace Audiola.Services;

/// <summary>Mikrofon-Aufnahme via NAudio <see cref="WaveInEvent"/> → 44,1 kHz/16-bit/Mono-WAV.</summary>
public sealed class AudioRecorder : IAudioRecorder
{
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private string? _path;
    private TaskCompletionSource<string>? _tcs;

    public bool IsRecording => _waveIn is not null;

    public string Start()
    {
        if (IsRecording) return _path!;

        _path = TempDir.File("rec", ".wav", "rec");

        _waveIn = new WaveInEvent { WaveFormat = new WaveFormat(44100, 16, 1) };
        _writer = new WaveFileWriter(_path, _waveIn.WaveFormat);
        _tcs = new TaskCompletionSource<string>();

        _waveIn.DataAvailable += OnData;
        _waveIn.RecordingStopped += OnStopped;
        _waveIn.StartRecording();
        return _path;
    }

    public Task<string> StopAsync()
    {
        if (!IsRecording) return Task.FromResult(_path ?? "");
        _waveIn!.StopRecording();
        return _tcs!.Task;
    }

    private void OnData(object? sender, WaveInEventArgs e) => _writer?.Write(e.Buffer, 0, e.BytesRecorded);

    private void OnStopped(object? sender, StoppedEventArgs e)
    {
        _writer?.Dispose();
        _writer = null;
        if (_waveIn is not null)
        {
            _waveIn.DataAvailable -= OnData;
            _waveIn.RecordingStopped -= OnStopped;
            _waveIn.Dispose();
            _waveIn = null;
        }
        _tcs?.TrySetResult(_path ?? "");
    }
}
