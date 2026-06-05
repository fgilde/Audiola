using Audiola.Models;

namespace Audiola.Services;

public sealed record VariationResult(
    double InputLufs,
    double OutputLufs,
    double AppliedLoudnessGainDb,
    int ClippedSamples,
    bool StereoWidthSkipped,
    int Seed);

public interface IVariationService
{
    /// <summary>
    /// Erzeugt eine subtile, reproduzierbare Variante des geladenen Tracks
    /// und exportiert sie ins gewuenschte Zielformat.
    /// </summary>
    Task<VariationResult> ProcessAndExportAsync(
        string inputFile,
        string outputFile,
        VariationSettings settings,
        CancellationToken ct = default);
}
