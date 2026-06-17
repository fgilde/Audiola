using Audiola.Models;

namespace Audiola.Services;

/// <summary>Ergebnis des Export-Dialogs: Zielpfad, Format-Bitrate, Tags und ob Lyrics eingebettet werden.</summary>
public sealed class ExportRequest
{
    public required string Path { get; init; }
    public int Bitrate { get; init; } = 256_000;
    public required AudioMetadata Metadata { get; init; }
    public bool EmbedLyrics { get; init; }
}
