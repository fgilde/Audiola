using System.IO;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Audiola.Services;

/// <summary>
/// Schreibt einen Sample-Stream je nach Dateiendung als WAV, MP3 oder AAC/M4A.
/// MP3/AAC laufen über Windows Media Foundation (auf Windows immer vorhanden).
/// </summary>
public static class AudioExporter
{
    /// <summary>Filter für den Windows-Speichern-Dialog (WAV als Standard).</summary>
    public const string SaveFilter =
        "WAV (*.wav)|*.wav|MP3 (*.mp3)|*.mp3|AAC / M4A (*.m4a)|*.m4a";

    private static bool _mfStarted;

    public static void Export(ISampleProvider source, string path, int bitrate = 256_000)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        switch (ext)
        {
            case ".mp3":
                EnsureMediaFoundation();
                MediaFoundationEncoder.EncodeToMp3(new SampleToWaveProvider16(source), path, bitrate);
                break;

            case ".m4a":
            case ".aac":
                EnsureMediaFoundation();
                MediaFoundationEncoder.EncodeToAac(new SampleToWaveProvider16(source), path, bitrate);
                break;

            case ".wav":
            default:
                WaveFileWriter.CreateWaveFile16(path, source);
                break;
        }
    }

    private static void EnsureMediaFoundation()
    {
        if (_mfStarted) return;
        MediaFoundationApi.Startup();
        _mfStarted = true;
    }
}
