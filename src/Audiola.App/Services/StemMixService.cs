using Audiola.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Audiola.Services;

/// <summary>Offline-Mixdown mehrerer Stems zu einer WAV-Datei.</summary>
public sealed class StemMixService : IStemMixService
{
    public Task ExportMixAsync(IReadOnlyList<Stem> stems, string outputPath, CancellationToken ct = default)
        => Task.Run(() => ExportMix(stems, outputPath, ct), ct);

    private static void ExportMix(IReadOnlyList<Stem> stems, string outputPath, CancellationToken ct)
    {
        var anySolo = stems.Any(s => s.IsSolo);
        var active = stems
            .Where(s => s.IsEnabled && !s.IsMuted && (!anySolo || s.IsSolo))
            .ToList();

        if (active.Count == 0)
            throw new InvalidOperationException("Keine aktiven Stems zum Mischen.");

        var readers = new List<AudioFileReader>();
        try
        {
            var inputs = new List<ISampleProvider>();
            foreach (var stem in active)
            {
                var reader = new AudioFileReader(stem.FilePath);
                readers.Add(reader);

                ISampleProvider provider = reader;
                // Auf Stereo normalisieren, damit das Panning konsistent ist.
                if (provider.WaveFormat.Channels == 1)
                    provider = new MonoToStereoSampleProvider(provider);

                inputs.Add(new StemMixSampleProvider(provider, stem.Volume, stem.Pan));
            }

            var mixer = new MixingSampleProvider(inputs) { ReadFully = false };
            AudioExporter.Export(mixer, outputPath);
        }
        finally
        {
            foreach (var r in readers)
                r.Dispose();
        }
    }
}
