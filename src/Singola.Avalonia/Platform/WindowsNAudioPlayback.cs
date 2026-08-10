using Audiola.Services.Audio;
using NAudio.Wave;

namespace Singola.Avalonia.Platform;

/// <summary>Windows playback adapter that retains NAudio's existing Media Foundation format support.</summary>
internal sealed class WindowsNAudioPlayback : IAudioPlayback
{
    private AudioFileReader? _reader;
    private WaveOutEvent? _output;
    private bool _disposed;

    public event EventHandler? PlaybackEnded;

    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;
    public TimeSpan Position => _reader?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan Duration => _reader?.TotalTime ?? TimeSpan.Zero;

    public void Open(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path)) throw new FileNotFoundException("Die Audiodatei wurde nicht gefunden.", path);

        Close();
        var reader = new AudioFileReader(path);
        var output = new WaveOutEvent { DesiredLatency = 120 };
        try
        {
            output.Init(reader);
            output.PlaybackStopped += OnPlaybackStopped;
            _reader = reader;
            _output = output;
        }
        catch
        {
            output.Dispose();
            reader.Dispose();
            throw;
        }
    }

    public void Play()
    {
        EnsureOpen();
        _output!.Play();
    }

    public void Pause()
    {
        if (!_disposed) _output?.Pause();
    }

    public void Stop()
    {
        if (!_disposed) Close();
    }

    public void Seek(TimeSpan position)
    {
        EnsureOpen();
        _reader!.CurrentTime = TimeSpan.FromTicks(Math.Clamp(position.Ticks, 0, Duration.Ticks));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Close();
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is null && _reader is { } reader && reader.CurrentTime >= reader.TotalTime)
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }

    private void Close()
    {
        if (_output is { } output)
        {
            output.PlaybackStopped -= OnPlaybackStopped;
            output.Stop();
            output.Dispose();
            _output = null;
        }

        _reader?.Dispose();
        _reader = null;
    }

    private void EnsureOpen()
    {
        if (_reader is null || _output is null)
            throw new InvalidOperationException("Vor dem Steuern der Wiedergabe muss eine Datei geöffnet werden.");
    }
}
