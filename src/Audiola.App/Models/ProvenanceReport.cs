namespace Audiola.Models;

public enum FindingSeverity
{
    /// <summary>Neutrale Information (Format, Encoder …).</summary>
    Info,

    /// <summary>Hinweis auf KI-Herkunft / Provenienz-Kennzeichnung.</summary>
    AiIndicator,

    /// <summary>Wichtig für die Bewertung der Erkennbarkeit.</summary>
    Notice
}

/// <summary>Ein einzelner Befund der Provenienz-/Metadaten-Analyse.</summary>
public sealed record Finding(
    string Category,
    string Title,
    string Detail,
    FindingSeverity Severity);

/// <summary>Ergebnis der Analyse einer Audiodatei.</summary>
public sealed class ProvenanceReport
{
    public required string FilePath { get; init; }
    public required IReadOnlyList<Finding> Findings { get; init; }

    /// <summary>Zusammenfassende, erklärende Bewertung (mehrzeilig).</summary>
    public required string Assessment { get; init; }

    public bool C2paToolAvailable { get; init; }

    /// <summary>Roh-Ausgabe von c2patool (falls vorhanden).</summary>
    public string? C2paRaw { get; init; }
}
