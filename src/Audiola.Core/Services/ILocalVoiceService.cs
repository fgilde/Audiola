using Audiola.Models;

namespace Audiola.Services;

/// <summary>Ein lokal verfügbares/installierbares Modell der Voice-Engine.</summary>
public sealed record LocalVoiceModel(
    string Id, string Name, string Description, string Capability, int SizeMb, bool Installed)
{
    /// <summary>Kann das Modell aus einem Referenz-Sample klonen?</summary>
    public bool CanClone => Capability is "clone" or "both";

    public string SizeText => SizeMb >= 1024 ? $"{SizeMb / 1024.0:0.#} GB" : $"{SizeMb} MB";
}

/// <summary>Ein Transkript-Abschnitt (Sekunden) für LRC/Untertitel.</summary>
public sealed record TranscriptSegment(double Start, double End, string Text);

/// <summary>
/// Lokale Voice-Engine über einen Python-Sidecar (VoiceBox-artig): Modelle herunterladen,
/// Text-zu-Sprache (optional mit Zero-Shot-Cloning) und Whisper-Transkription — auf CUDA oder CPU.
/// </summary>
public interface ILocalVoiceService
{
    /// <summary>True, wenn das Sidecar-Skript vorhanden ist.</summary>
    bool ScriptAvailable { get; }

    /// <summary>Liefert die Modell-Liste (mit Installations-Status). Fällt auf eine eingebaute Liste zurück.</summary>
    Task<IReadOnlyList<LocalVoiceModel>> GetModelsAsync(CancellationToken ct = default);

    /// <summary>Liefert die verfügbaren Whisper-Transkriptionsmodelle (mit Installations-Status).</summary>
    Task<IReadOnlyList<LocalVoiceModel>> GetWhisperModelsAsync(CancellationToken ct = default);

    /// <summary>Lädt ein Modell herunter / installiert es lokal.</summary>
    Task DownloadModelAsync(string modelId, IProgress<string>? progress = null, CancellationToken ct = default);

    /// <summary>Text-zu-Sprache mit einem lokalen Profil; liefert Stereo-Float + Samplerate.</summary>
    Task<(float[] Samples, int SampleRate)> SpeakAsync(string text, VoiceProfile profile, double speed, CancellationToken ct = default);

    /// <summary>Speech-to-Speech (Stimmtausch) lokal — wirft, falls (noch) nicht unterstützt.</summary>
    Task<(float[] Samples, int SampleRate)> ChangeVoiceAsync(string inputWav, VoiceProfile profile, CancellationToken ct = default);

    /// <summary>Transkribiert eine Audiodatei via Whisper in Zeit-Segmente.</summary>
    Task<IReadOnlyList<TranscriptSegment>> TranscribeAsync(string inputWav, string whisperModel, CancellationToken ct = default);
}
