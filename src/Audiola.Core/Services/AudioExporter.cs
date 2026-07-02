using System.IO;
using CUETools.Codecs;
using CUETools.Codecs.FLAKE;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Audiola.Services;

/// <summary>
/// Schreibt einen Sample-Stream je nach Dateiendung als WAV, MP3, AAC/M4A oder FLAC.
/// MP3/AAC laufen über Windows Media Foundation (auf Windows immer vorhanden),
/// FLAC über den rein verwalteten FLAKE-Encoder (CUETools.Codecs.FLAKE) – damit
/// unabhängig vom unzuverlässigen MF-FLAC-Encoder (MF_E_INVALIDMEDIATYPE).
/// </summary>
public static class AudioExporter
{
    /// <summary>Filter für den Windows-Speichern-Dialog (WAV als Standard).</summary>
    public const string SaveFilter =
        "WAV (*.wav)|*.wav|MP3 (*.mp3)|*.mp3|AAC / M4A (*.m4a)|*.m4a|FLAC (*.flac)|*.flac";

    private static bool _mfStarted;

    /// <summary>Bequem-Overload: schreibt einen Float-Puffer (interleaved) direkt als Datei.</summary>
    public static void Export(float[] samples, int sampleRate, int channels, string path, int bitrate = 256_000)
        => Export(new FloatArraySampleProvider(samples, sampleRate, channels), path, bitrate);

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
                EncodeFlac(source, path);
                break;

            case ".wav":
            default:
                WaveFileWriter.CreateWaveFile16(path, source);
                break;
        }
    }

    /// <summary>
    /// Verlustfreies FLAC über den rein verwalteten FLAKE-Encoder (CUETools.Codecs.FLAKE).
    /// Liest den kompletten Float-Sample-Stream (IEEE float, interleaved, -1..1), wandelt ihn
    /// blockweise in 16-Bit-PCM um und schreibt eine gültige .flac-Datei.
    /// </summary>
    private static void EncodeFlac(ISampleProvider source, string path)
    {
        var fmt = source.WaveFormat;
        if (fmt.Channels < 1)
            throw new InvalidOperationException("Ungültiges Audioformat für FLAC-Export (keine Kanäle).");

        var pcm = new AudioPCMConfig(16, fmt.Channels, fmt.SampleRate);

        // Float-Eingabe blockweise lesen und in das FLAKE-int[frame, channel]-Layout umwandeln.
        const int framesPerBlock = 4096;
        var floatBuffer = new float[framesPerBlock * fmt.Channels];

        FlakeWriter? writer = null;
        try
        {
            writer = new FlakeWriter(path, pcm) { CompressionLevel = 5 };

            int floatsRead;
            while ((floatsRead = source.Read(floatBuffer, 0, floatBuffer.Length)) > 0)
            {
                int frames = floatsRead / fmt.Channels;
                if (frames == 0)
                    continue;

                var block = new AudioBuffer(pcm, frames);
                var samples = block.Samples; // int[frames, channels]
                int idx = 0;
                for (int frame = 0; frame < frames; frame++)
                {
                    for (int ch = 0; ch < fmt.Channels; ch++)
                    {
                        float x = floatBuffer[idx++];
                        int s = (int)Math.Round(x * 32767f);
                        if (s > 32767) s = 32767;
                        else if (s < -32768) s = -32768;
                        samples[frame, ch] = s;
                    }
                }
                block.Length = frames;
                writer.Write(block);
            }
        }
        finally
        {
            // Close() schreibt den Stream-Footer/MD5 und schließt die Datei – muss laufen,
            // damit die .flac-Datei gültig ist.
            writer?.Close();
        }
    }

    private static void EnsureMediaFoundation()
    {
        if (_mfStarted) return;
        MediaFoundationApi.Startup();
        _mfStarted = true;
    }
}
