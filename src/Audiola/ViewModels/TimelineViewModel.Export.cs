using System.Collections.ObjectModel;
using System.Windows;
using Audiola.Helper;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Audiola.ViewModels;

public sealed partial class TimelineViewModel
{
    [RelayCommand(CanExecute = nameof(CanExportRange))]
    private async Task ExportRangeAsync()
    {
        var start = TimeSpan.FromSeconds(SelectionStartSeconds);
        var end = TimeSpan.FromSeconds(SelectionEndSeconds);
        var tracks = Tracks.ToList();
        await Export.ExportAsync(
            DefaultExportName("bereich"),
            () => Task.Run(() => _engine.RenderRange(tracks, start, end)),
            SongMeta.ToMetadata());
    }

    /// <summary>
    /// Rendert den aktuellen Studio-Mix (alle aktiven Spuren mit allen Bearbeitungen) in eine
    /// temporäre WAV und gibt deren Pfad zurück, oder <c>null</c> wenn keine Spuren vorhanden sind.
    /// Wird z. B. von der Provenienz-Analyse genutzt, wenn kein einzelner Track geladen ist.
    /// </summary>
    public async Task<string?> RenderMixToTempFileAsync()
    {
        if (Tracks.Count == 0) return null;
        var end = TimeSpan.FromSeconds(DurationSeconds);
        var tracks = Tracks.ToList();
        return await Task.Run(() =>
        {
            var (samples, sr) = _engine.RenderRange(tracks, TimeSpan.Zero, end);
            var path = TempDir.File("render", ".wav", "mix");
            AudioExporter.Export(new FloatArraySampleProvider(samples, sr, 2), path);
            return path;
        });
    }

    /// <summary>
    /// Exportiert den kompletten Studio-Mix so, wie er gerade klingt — alle aktiven Spuren,
    /// mit Lautstärke/Pan/Fades, Stimmtausch, Live-EQ und Mastering. Spart den Umweg über
    /// die Mastering-Seite.
    /// </summary>
    [RelayCommand]
    private async Task ExportMixAsync()
    {
        if (Tracks.Count == 0)
        {
            _snackbar.Warning("Nichts zu exportieren", "Es sind keine Spuren im Studio.");
            return;
        }

        var end = TimeSpan.FromSeconds(DurationSeconds);
        var tracks = Tracks.ToList();
        await Export.ExportAsync(
            DefaultExportName("mix"),
            () => Task.Run(() => _engine.RenderRange(tracks, TimeSpan.Zero, end)),
            SongMeta.ToMetadata(),
            generateLyrics: GenerateMixLyricsAsync,
            elevenLabsAvailable: ElevenLabsAvailable);
    }

    /// <summary>Erzeugt Lyrics für den gesamten Mix — für den „erzeugen"-Button im Export-Dialog (lokal oder ElevenLabs).</summary>
    private async Task<string?> GenerateMixLyricsAsync(bool useElevenLabs)
    {
        var mix = await RenderMixToTempFileAsync();
        return mix is null ? null : await TranscribeFileToLrcAsync(mix,
            string.IsNullOrWhiteSpace(SongMeta.Title) ? null : SongMeta.Title, useElevenLabs);
    }

    /// <summary>Exportiert eine einzelne Spur (gerendert mit ihrer Lautstärke/Pan) als Audiodatei.</summary>
    [RelayCommand]
    private async Task ExportTrack(StemTrackViewModel? track)
    {
        if (track is null) return;
        var end = track.Clips.Count > 0 ? track.Clips.Max(c => c.EndSeconds) : track.LengthSeconds;
        if (end <= 0.01) return;

        var single = new List<StemTrackViewModel> { track };
        // Tags aus dem Projekt übernehmen, aber den Titel auf den Spurnamen setzen, falls noch keiner da ist.
        var seed = SongMeta.ToMetadata();
        if (string.IsNullOrWhiteSpace(seed.Title)) seed.Title = track.Name;
        var sourcePath = track.Clips.FirstOrDefault()?.SourcePath;

        await Export.ExportAsync(
            track.Name,
            () => Task.Run(() => _engine.RenderRange(single, TimeSpan.Zero, TimeSpan.FromSeconds(end))),
            seed,
            seedLyrics: track.Lrc,
            generateLyrics: string.IsNullOrEmpty(sourcePath) ? null
                : useEleven => TranscribeFileToLrcAsync(sourcePath!, track.Name, useEleven),
            elevenLabsAvailable: ElevenLabsAvailable);
    }
}
