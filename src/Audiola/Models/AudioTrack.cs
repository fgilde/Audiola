using System.IO;

namespace Audiola.Models;

/// <summary>
/// Eine geladene Audiodatei inklusive aufbereiteter Wellenform-Peaks.
/// </summary>
public sealed class AudioTrack
{
    public required string FilePath { get; init; }

    public string FileName => Path.GetFileName(FilePath);

    /// <summary>Gesamtdauer des Tracks.</summary>
    public TimeSpan Duration { get; init; }

    public int SampleRate { get; init; }

    public int Channels { get; init; }

    /// <summary>
    /// Normalisierte Min/Max-Peaks pro Pixel-Bucket fuer die Wellenform-Darstellung.
    /// Werte liegen im Bereich [-1, 1].
    /// </summary>
    public float[] Peaks { get; init; } = [];
}
