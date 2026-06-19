namespace Audiola.Services;

/// <summary>Ein hochwertiges Trennungs-Modell (UVR/audio-separator).</summary>
public sealed record SeparationModel(string Key, string Name, string Description, string ModelFilename);

/// <summary>Ein getrennter Stem (Datei + Bezeichnung wie „Vocals“, „Instrumental“, „Lead Vocals“).</summary>
public sealed record SeparatedStem(string FilePath, string Name);

/// <summary>
/// Hochwertige Stem-Trennung über das Python-Paket <c>audio-separator</c> (UVR-Modelle:
/// BS/Mel-Band-RoFormer, Demucs, Karaoke-Lead/Background) — auf CUDA, im verwalteten venv.
/// </summary>
public interface IAdvancedSeparationService
{
    bool ScriptAvailable { get; }

    /// <summary>Verfügbare Trennungs-Modelle.</summary>
    IReadOnlyList<SeparationModel> Models { get; }

    /// <summary>Trennt die Datei mit dem gewählten Modell; liefert die erzeugten Stem-Dateien.</summary>
    Task<IReadOnlyList<SeparatedStem>> SeparateAsync(string inputFile, string modelFilename,
        IProgress<string>? progress = null, CancellationToken ct = default);

    /// <summary>Stellt die Stem-Trennung bereit (venv + audio-separator) — für den Einrichtungs-Assistenten.</summary>
    Task EnsureInstalledAsync(IProgress<string>? progress = null, CancellationToken ct = default);
}
