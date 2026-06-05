namespace Audiola.Services;

/// <summary>Eine im Dienst verfügbare Stimme.</summary>
public sealed record VoiceInfo(string Id, string Name, string Category)
{
    /// <summary>Anzeigename inkl. Kategorie (für Dropdowns).</summary>
    public string Display => string.IsNullOrEmpty(Category) ? Name : $"{Name}  ({Category})";
}

/// <summary>
/// Tausch/Veredelung einer Stimme über einen externen Dienst (Speech-to-Speech).
/// Die Performance (Timing/Betonung) der Eingabe bleibt erhalten, die Klangfarbe
/// wird durch die Zielstimme ersetzt.
/// </summary>
public interface IVoiceChangeService
{
    /// <summary>True, wenn API-Key und Ziel-Voice-ID hinterlegt sind.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Schickt die Audiodatei an den Dienst und liefert das Ergebnis als
    /// interleaved Stereo-Float-Puffer samt Samplerate zurück.
    /// </summary>
    Task<(float[] Samples, int SampleRate)> ChangeAsync(string inputWavPath, CancellationToken ct = default);

    /// <summary>Liste der verfügbaren Stimmen des Kontos (für die Auswahl).</summary>
    Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Erstellt aus einem oder mehreren Audio-Samples eine neue (geklonte) Stimme
    /// und liefert deren Voice-ID. Nur für eigene/lizenzierte Stimmen verwenden.
    /// </summary>
    Task<string> CreateVoiceFromSamplesAsync(string name, IReadOnlyList<string> samplePaths, CancellationToken ct = default);
}
