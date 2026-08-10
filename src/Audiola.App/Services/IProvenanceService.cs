using Audiola.Models;

namespace Audiola.Services;

public interface IProvenanceService
{
    /// <summary>
    /// Untersucht eine Audiodatei auf eingebettete Provenienz-/Herkunftsdaten
    /// (C2PA / Content Credentials, XMP, ID3, Encoder-Spuren) und erstellt eine
    /// erklärende Bewertung der KI-Erkennbarkeit. Reine Lese-/Analysefunktion.
    /// </summary>
    Task<ProvenanceReport> AnalyzeAsync(string filePath, CancellationToken ct = default);
}
