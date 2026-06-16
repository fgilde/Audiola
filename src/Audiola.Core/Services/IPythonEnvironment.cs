namespace Audiola.Services;

/// <summary>
/// Verwaltet eine eigene, isolierte Python-Umgebung (venv) für die lokale Voice-Engine,
/// damit das Programm alle benötigten Pakete selbst sicherstellt (kein manuelles pip).
/// </summary>
public interface IPythonEnvironment
{
    /// <summary>Pfad zur python-Executable der verwalteten Umgebung.</summary>
    string PythonExe { get; }

    /// <summary>True, wenn die verwaltete Umgebung bereits existiert.</summary>
    bool Exists { get; }

    /// <summary>Legt die Umgebung an (falls nötig) und aktualisiert pip.</summary>
    Task EnsureAsync(IProgress<string>? progress = null, CancellationToken ct = default);

    /// <summary>Installiert Pakete in die verwaltete Umgebung (optional von einem Index-URL).</summary>
    Task InstallAsync(IReadOnlyList<string> packages, string? indexUrl = null,
        IProgress<string>? progress = null, CancellationToken ct = default);

    /// <summary>Installiert aus einer requirements.txt (für repo-basierte Modelle wie seed-vc).</summary>
    Task InstallRequirementsAsync(string requirementsFile, IProgress<string>? progress = null, CancellationToken ct = default);
}
