namespace Audiola.Services;

/// <summary>Eine im Dienst verfügbare Stimme.</summary>
public sealed record VoiceInfo(string Id, string Name, string Category)
{
    /// <summary>Anzeigename inkl. Kategorie (für Dropdowns).</summary>
    public string Display => string.IsNullOrEmpty(Category) ? Name : $"{Name}  ({Category})";

    /// <summary>Deutsche Kategorie-Überschrift für die Gruppierung im Dropdown.</summary>
    public string CategoryLabel => Category switch
    {
        "premade" => "Standard",
        "cloned" => "Eigene (geklont)",
        "professional" => "Professionell",
        "generated" => "Generiert",
        "famous" => "Prominente",
        _ => string.IsNullOrEmpty(Category) ? "Sonstige" : Category
    };
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

    /// <summary>True, wenn (mindestens) ein API-Key hinterlegt ist.</summary>
    bool HasApiKey { get; }

    /// <summary>
    /// Schickt die Audiodatei an den Dienst und liefert das Ergebnis als
    /// interleaved Stereo-Float-Puffer samt Samplerate zurück (Ziel = Stimme aus den Einstellungen).
    /// </summary>
    Task<(float[] Samples, int SampleRate)> ChangeAsync(string inputWavPath, CancellationToken ct = default);

    /// <summary>Speech-to-Speech mit explizit gewählter Zielstimme.</summary>
    Task<(float[] Samples, int SampleRate)> ChangeAsync(string inputWavPath, string voiceId, CancellationToken ct = default);

    /// <summary>Text-to-Speech mit gewählter Stimme; liefert Stereo-Float + Samplerate.</summary>
    Task<(float[] Samples, int SampleRate)> SpeakAsync(string text, string voiceId,
        double speed, double stability, double similarity, CancellationToken ct = default);

    /// <summary>
    /// Transkribiert eine Audiodatei über den Dienst (ElevenLabs Speech-to-Text/Scribe) und
    /// liefert zeitgestempelte Segmente — Alternative zur lokalen Whisper-Transkription.
    /// </summary>
    Task<IReadOnlyList<TranscriptSegment>> TranscribeAsync(string audioPath, CancellationToken ct = default);

    /// <summary>Löscht eine (temporär geklonte) Stimme wieder aus dem Konto.</summary>
    Task DeleteVoiceAsync(string voiceId, CancellationToken ct = default);

    /// <summary>Liste der verfügbaren Stimmen des Kontos (für die Auswahl).</summary>
    Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Erstellt aus einem oder mehreren Audio-Samples eine neue (geklonte) Stimme
    /// und liefert deren Voice-ID. Nur für eigene/lizenzierte Stimmen verwenden.
    /// </summary>
    Task<string> CreateVoiceFromSamplesAsync(string name, IReadOnlyList<string> samplePaths, CancellationToken ct = default);
}
