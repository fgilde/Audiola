namespace Audiola.Services;

/// <summary>Eine benannte, anwendbare Variation/Bearbeitung eines Providers.</summary>
public sealed record AudioVariation(string Id, string Name, string Description);

/// <summary>
/// Ein Provider, der benannte Audio-Variationen anbieten und auf einen interleaved
/// Stereo-Float-Puffer anwenden kann. Mehrere Implementierungen können registriert sein
/// und werden über <see cref="Name"/> unterschieden (z. B. "Studio-Effekte", "Internal").
/// </summary>
public interface IAudioVariationProvider
{
    /// <summary>Eindeutiger Anzeigename des Providers.</summary>
    string Name { get; }

    /// <summary>Die Variationen, die dieser Provider anbieten kann.</summary>
    IReadOnlyList<AudioVariation> GetVariations();

    /// <summary>
    /// Wendet eine Variation auf einen interleaved Stereo-Float-Puffer an und liefert das Ergebnis.
    /// Mehrere Variationen werden vom Aufrufer nacheinander angewendet (verkettet).
    /// </summary>
    Task<float[]> ApplyAsync(string variationId, float[] interleavedStereo, int sampleRate, CancellationToken ct = default);
}
