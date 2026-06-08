using Audiola.Models;

namespace Audiola.Services;

public interface IWaveformService
{
    /// <summary>
    /// Liest eine Audiodatei und erzeugt ein <see cref="AudioTrack"/> mit
    /// heruntergerechneten Peaks fuer die Wellenform-Anzeige.
    /// </summary>
    /// <param name="targetBuckets">Gewuenschte Anzahl an Peak-Buckets (z. B. Pixelbreite).</param>
    Task<AudioTrack> LoadAsync(string filePath, int targetBuckets = 2000, CancellationToken ct = default);
}
