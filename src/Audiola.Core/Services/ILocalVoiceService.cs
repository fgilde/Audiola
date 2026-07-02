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

/// <summary>Status der lokalen GPU-Unterstützung (torch/CUDA).</summary>
public sealed record GpuStatus(bool TorchInstalled, bool CudaAvailable, string DeviceName, string? Detail)
{
    public string Summary => !TorchInstalled
        ? "torch noch nicht installiert — Modell „Laden“ oder „CUDA-Torch installieren“."
        : CudaAvailable
            ? $"CUDA aktiv: {DeviceName}"
            : "CUDA NICHT verfügbar — es läuft auf CPU (langsam). „CUDA-Torch installieren“ versuchen.";
}

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

    /// <summary>Prüft torch/CUDA in der verwalteten Umgebung.</summary>
    Task<GpuStatus> CheckGpuAsync(CancellationToken ct = default);

    /// <summary>Installiert die CUDA-Variante von torch/torchaudio in die verwaltete Umgebung.</summary>
    Task InstallCudaTorchAsync(IProgress<string>? progress = null, CancellationToken ct = default);

    /// <summary>Text-zu-Sprache mit einem lokalen Profil; liefert Stereo-Float + Samplerate.</summary>
    Task<(float[] Samples, int SampleRate)> SpeakAsync(string text, VoiceProfile profile, double speed, CancellationToken ct = default);

    /// <summary>
    /// Speech-to-Speech (Stimmtausch) lokal über seed-vc; meldet Fortschritt live.
    /// Mehr <paramref name="diffusionSteps"/> = mehr Detail/Ausdruck; <paramref name="autoF0Adjust"/>=false
    /// behält die Original-Melodie/Betonung exakt.
    /// </summary>
    Task<(float[] Samples, int SampleRate)> ChangeVoiceAsync(string inputWav, VoiceProfile profile,
        int diffusionSteps = 50, bool autoF0Adjust = false,
        IProgress<string>? progress = null, CancellationToken ct = default);

    /// <summary>Transkribiert eine Audiodatei via Whisper in Zeit-Segmente.</summary>
    Task<IReadOnlyList<TranscriptSegment>> TranscribeAsync(string inputWav, string whisperModel, CancellationToken ct = default);

    /// <summary>
    /// Tonhöhen-Korrektur (Auto-Tune): zieht die Aufnahme (<paramref name="inputWav"/>) auf die
    /// Melodie der Referenz (<paramref name="referenceWav"/>) und behält die eigene Klangfarbe
    /// (WORLD-Vocoder). <paramref name="strength"/> 0..1 blendet die Korrektur.
    /// </summary>
    Task<(float[] Samples, int SampleRate)> AutoTuneAsync(string inputWav, string referenceWav,
        double strength, IProgress<string>? progress = null, CancellationToken ct = default);
}
