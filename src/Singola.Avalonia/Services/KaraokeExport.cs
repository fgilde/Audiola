using System.IO;
using Audiola.Services;
using Audiola.Services.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Singola.Services;

/// <summary>
/// Mixes the recorded microphone tracks of a round back over the song and writes the result as
/// WAV, MP3, M4A, or FLAC (the format follows the file extension, see <see cref="AudioExporter"/>).
/// One voice yields a solo take, several voices a duet.
/// </summary>
public static class KaraokeExport
{
    private const int Rate = 44100;

    /// <summary>File types offered in the save dialog; MP3 first because it is the shareable one.</summary>
    public static readonly string[] Extensions = ["mp3", "wav", "m4a", "flac"];

    /// <summary>True when at least one of the recordings actually holds audio.</summary>
    public static bool HasAudio(string? path) =>
        path is not null && File.Exists(path) && new FileInfo(path).Length > 4096;

    /// <summary>
    /// Writes <paramref name="songPath"/> mixed with every recording in <paramref name="voicePaths"/>
    /// to <paramref name="outputPath"/>. The song is the lead track: the mix ends when it ends.
    /// </summary>
    public static void Save(string songPath, IReadOnlyList<string> voicePaths, string outputPath,
        float songGain = 0.72f, float voiceGain = 1.25f)
    {
        var voices = voicePaths.Where(HasAudio).ToList();
        if (voices.Count == 0) throw new InvalidOperationException("Für diese Runde gibt es keine Aufnahme.");

        var readers = new List<AudioFileReader>();
        try
        {
            var song = PortableAudioFile.Open(songPath);
            readers.Add(song);
            song.Volume = songGain;
            var songSeconds = song.TotalTime.TotalSeconds;

            var mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(Rate, 2))
            {
                // Ohne ReadFully endet der Mix, sobald die kürzeste Stimme aus ist.
                ReadFully = true
            };
            mixer.AddMixerInput(ToStereo(song));

            // Mehrere Stimmen teilen sich den Kopfraum, damit ein Duett nicht lauter clippt als ein Solo.
            var perVoice = voiceGain / (float)Math.Sqrt(voices.Count);
            foreach (var path in voices)
            {
                var voice = new AudioFileReader(path) { Volume = perVoice };
                readers.Add(voice);
                mixer.AddMixerInput(ToStereo(voice));
            }

            var mix = new OffsetSampleProvider(mixer) { Take = TimeSpan.FromSeconds(songSeconds) };
            AudioExporter.Export(Normalize(mix, songSeconds), Rate, 2, outputPath);
        }
        finally
        {
            foreach (var reader in readers) reader.Dispose();
        }
    }

    /// <summary>
    /// Reads the whole mix so a too-hot sum can be scaled down instead of clipping — a shared
    /// recording should not distort.
    /// ponytail: hält den Song im Speicher (3 Minuten Stereo ≈ 60 MB); erst wenn jemand
    /// Stundenmitschnitte exportiert, lohnt ein Zwei-Pass-Stream.
    /// </summary>
    private static float[] Normalize(ISampleProvider mix, double seconds)
    {
        var samples = new List<float>(Math.Max(Rate, (int)(seconds * Rate * 2)));
        var buffer = new float[Rate * 2];
        int read;
        var peak = 0f;
        while ((read = mix.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (var i = 0; i < read; i++)
            {
                var sample = buffer[i];
                var magnitude = Math.Abs(sample);
                if (magnitude > peak) peak = magnitude;
                samples.Add(sample);
            }
        }

        var result = samples.ToArray();
        if (peak > 0.99f)
        {
            var scale = 0.99f / peak;
            for (var i = 0; i < result.Length; i++) result[i] *= scale;
        }

        return result;
    }

    /// <summary>Brings any input to the 44.1 kHz stereo mixer format.</summary>
    private static ISampleProvider ToStereo(ISampleProvider provider)
    {
        if (provider.WaveFormat.SampleRate != Rate)
            provider = new WdlResamplingSampleProvider(provider, Rate);

        return provider.WaveFormat.Channels switch
        {
            1 => new MonoToStereoSampleProvider(provider),
            2 => provider,
            _ => new MultiplexingSampleProvider([provider], 2),
        };
    }
}
