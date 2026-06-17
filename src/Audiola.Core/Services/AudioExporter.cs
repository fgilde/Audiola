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
        "WAV (*.wav)|*.wav|MP3 (*.mp3)|*.mp3|AAC / M4A (*.m4a)|*.m4a|FLAC (*.flac)|*.flac";

    /// <summary>FLAC-Subtyp-GUID für Media Foundation (verlustfreier FLAC-Encoder, Windows 10+).</summary>
    private static readonly Guid MFAudioFormat_FLAC = new("0000F1AC-0000-0010-8000-00AA00389B71");

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

            case ".flac":
                EnsureMediaFoundation();
                EncodeFlac(new SampleToWaveProvider16(source), path);
                break;

            case ".wav":
            default:
                WaveFileWriter.CreateWaveFile16(path, source);
                break;
        }
    }

    /// <summary>Verlustfreies FLAC über den Media-Foundation-FLAC-Encoder (Windows 10+).</summary>
    private static void EncodeFlac(IWaveProvider source, string path)
    {
        var mediaType = MediaFoundationEncoder.SelectMediaType(MFAudioFormat_FLAC, source.WaveFormat, 0)
            ?? throw new InvalidOperationException(
                "Der FLAC-Encoder (Media Foundation) ist auf diesem System nicht verfügbar. " +
                "Bitte WAV, MP3 oder M4A wählen.");
        using var encoder = new MediaFoundationEncoder(mediaType);
        encoder.Encode(path, source);
    }

    private static void EnsureMediaFoundation()
    {
        if (_mfStarted) return;
        MediaFoundationApi.Startup();
        _mfStarted = true;
    }
}
