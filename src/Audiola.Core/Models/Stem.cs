using System.IO;

namespace Audiola.Models;

/// <summary>Die von Demucs unterstützten Stems (htdemucs = 4, htdemucs_6s = +Guitar/Piano).</summary>
public enum StemKind
{
    Vocals,
    Drums,
    Bass,
    Guitar,
    Piano,
    Other
}

/// <summary>Ein einzelner extrahierter Stem mit Mix-Parametern.</summary>
public sealed class Stem
{
    public required StemKind Kind { get; init; }

    public required string FilePath { get; init; }

    public string FileName => Path.GetFileName(FilePath);

    /// <summary>Lautstaerke 0.0 - 1.5 (1.0 = Originalpegel).</summary>
    public float Volume { get; set; } = 1.0f;

    /// <summary>Panorama -1.0 (links) .. 0 (mitte) .. 1.0 (rechts).</summary>
    public float Pan { get; set; } = 0.0f;

    /// <summary>Checkbox in der Mixer-Liste: ist der Stem überhaupt aktiv?</summary>
    public bool IsEnabled { get; set; } = true;

    public bool IsMuted { get; set; }

    public bool IsSolo { get; set; }
}

/// <summary>Ergebnis einer Stem-Trennung.</summary>
public sealed class StemSet
{
    public required string SourceTrackPath { get; init; }

    public required IReadOnlyList<Stem> Stems { get; init; }

    public required string OutputDirectory { get; init; }
}
