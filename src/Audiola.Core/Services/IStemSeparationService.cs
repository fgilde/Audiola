using Audiola.Models;

namespace Audiola.Services;

public interface IStemSeparationService
{
    /// <summary>Prueft, ob Demucs ueber den konfigurierten Python-Pfad erreichbar ist.</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>
    /// Trennt eine Audiodatei in Stems (Vocals/Drums/Bass/Other).
    /// </summary>
    /// <param name="progress">Fortschrittsmeldungen aus der Demucs-Ausgabe.</param>
    /// <param name="modelOverride">Optionales Modell (z. B. "htdemucs_6s" für Gitarre/Piano); sonst aus den Einstellungen.</param>
    /// <param name="shifts">Test-Time-Augmentation für sauberere Trennung (0 = aus; höher = besser, aber langsamer).</param>
    Task<StemSet> SeparateAsync(
        string inputFile,
        IProgress<string>? progress = null,
        string? modelOverride = null,
        int shifts = 0,
        CancellationToken ct = default);
}
