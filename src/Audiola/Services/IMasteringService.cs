using Audiola.Models;

namespace Audiola.Services;

public sealed record MasteringResult(
    double InputLufs,
    double OutputLufs,
    double AppliedGainDb,
    int ClippedSamples);

public interface IMasteringService
{
    /// <summary>Misst die integrierte Lautheit (LUFS) einer Datei.</summary>
    Task<double> MeasureLufsAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Wendet EQ → Kompressor → Makeup → optionale LUFS-Normalisierung an
    /// und schreibt das Ergebnis als 16-bit-WAV.
    /// </summary>
    Task<MasteringResult> ProcessAndExportAsync(
        string inputFile,
        string outputFile,
        MasteringSettings settings,
        CancellationToken ct = default);

    /// <summary>Verarbeitet die Datei und liefert die Wellenform-Peaks des gemasterten Ergebnisses.</summary>
    Task<float[]> ProcessPeaksAsync(
        string inputFile,
        MasteringSettings settings,
        int targetBuckets = 2000,
        CancellationToken ct = default);
}
