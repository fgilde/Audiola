namespace Audiola.Models;

/// <summary>
/// Eine gespeicherte Stimme — bewusst getrennt vom Audioprojekt abgelegt
/// (eigener Stimmen-Store), damit Stimmen projektübergreifend wiederverwendbar sind.
/// </summary>
public sealed class VoiceProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "";

    /// <summary>"local" (Python-Sidecar) oder "elevenlabs".</summary>
    public string Engine { get; set; } = "local";

    /// <summary>Modell-ID der lokalen Engine (z. B. "xtts-v2", "kokoro") — nur bei Engine=local.</summary>
    public string ModelId { get; set; } = "";

    /// <summary>Sprache (ISO, z. B. "de", "en") — sofern vom Modell genutzt.</summary>
    public string Language { get; set; } = "de";

    public string Description { get; set; } = "";

    /// <summary>Referenz-Audiodateien (für Zero-Shot-Cloning), in den Stimmen-Store kopiert.</summary>
    public List<string> SamplePaths { get; set; } = [];

    /// <summary>ElevenLabs-Voice-ID — nur bei Engine=elevenlabs.</summary>
    public string ElevenVoiceId { get; set; } = "";

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public bool IsLocal => string.Equals(Engine, "local", StringComparison.OrdinalIgnoreCase);
}
