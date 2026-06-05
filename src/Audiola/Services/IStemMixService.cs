using Audiola.Models;

namespace Audiola.Services;

public interface IStemMixService
{
    /// <summary>
    /// Mischt die uebergebenen Stems unter Beruecksichtigung von Volume/Pan/Mute/Solo
    /// und schreibt das Ergebnis als WAV-Datei.
    /// </summary>
    Task ExportMixAsync(IReadOnlyList<Stem> stems, string outputPath, CancellationToken ct = default);
}
