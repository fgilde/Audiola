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
    /// <summary>Schnelle Trennung in 4 Stems (Modell aus Einstellungen, keine Inhaltserkennung).</summary>
    [RelayCommand]
    private Task SeparateTrack(StemTrackViewModel? track) => SeparateTrackCore(track, null, 0, detectContent: false);

    /// <summary>
    /// Automatik: trennt mit htdemucs_6s in höherer Qualität (shifts) und übernimmt nur die
    /// Stems, die tatsächlich hörbaren Inhalt haben (erkennt, was im Song steckt).
    /// </summary>
    [RelayCommand]
    private Task SeparateTrackAuto(StemTrackViewModel? track) => SeparateTrackCore(track, "htdemucs_6s", 2, detectContent: true);

    private async Task SeparateTrackCore(StemTrackViewModel? track, string? modelOverride, int shifts, bool detectContent)
    {
        if (track is null || IsSeparating) return;
        var clip = track.Clips.FirstOrDefault();
        if (clip is null) return;

        var path = clip.SourcePath;
        var offset = clip.TimelineOffsetSeconds;

        IsSeparating = true;
        try
        {
            if (!await _separation.IsAvailableAsync())
            {
                _snackbar.Warning("Demucs fehlt", "Bitte 'pip install demucs soundfile' ausführen.", 5);
                return;
            }

            SeparationStatus = "Stems trennen … 0 %";
            var progress = new Progress<string>(line =>
            {
                var m = System.Text.RegularExpressions.Regex.Match(line, @"(\d{1,3})%");
                if (m.Success) SeparationStatus = $"Stems trennen … {m.Groups[1].Value} %";
            });

            var result = await _separation.SeparateAsync(path, progress, modelOverride, shifts);

            // Inhaltserkennung: nur Stems mit hörbarem Pegel übernehmen.
            var stems = result.Stems.AsEnumerable();
            if (detectContent)
            {
                SeparationStatus = "Inhalt erkennen …";
                var measured = await Task.Run(() => result.Stems
                    .Select(s => (Stem: s, Rms: AudioProcessingHelper.MeasureRmsDb(s.FilePath)))
                    .ToList());
                var kept = measured.Where(x => x.Rms > StemPresenceDb).Select(x => x.Stem).ToList();
                stems = kept.Count > 0 ? kept : result.Stems; // Fallback: nie alles verwerfen
            }
            var stemList = stems.ToList();

            var insertAt = Tracks.IndexOf(track) + 1;
            foreach (var stem in stemList)
            {
                var data = await _waveform.LoadAsync(stem.FilePath, 4000);
                var dur = data.Duration.TotalSeconds;
                var t = StemTrackViewModel.ForFile(stem.FilePath, $"{StemName(stem.Kind)} ◂ {track.Name}", StemColor(stem.Kind));
                t.LengthSeconds = dur;
                t.Clips.Add(new ClipViewModel
                {
                    Track = t,
                    SourcePath = stem.FilePath,
                    SourceTotalSeconds = dur,
                    SourcePeaks = data.Peaks,
                    TimelineOffsetSeconds = offset,
                    SourceStartSeconds = 0,
                    LengthSeconds = dur,
                    Peaks = data.Peaks
                });
                Tracks.Insert(insertAt++, t);
            }

            OnPropertyChanged(nameof(HasTracks));
            RecomputeDuration();
            CommitClips();
            Commit("Stems getrennt");
            var detected = string.Join(", ", stemList.Select(s => StemName(s.Kind)));
            _snackbar.Success("Stems hinzugefügt",
                detectContent ? $"Erkannt: {detected} ({stemList.Count} Spuren)." : $"{stemList.Count} Spuren aus „{track.Name}“.", 4);
        }
        catch (Exception ex)
        {
            _snackbar.Error("Trennung fehlgeschlagen", ex.Message, 5);
        }
        finally
        {
            IsSeparating = false;
            SeparationStatus = "";
        }
    }

    /// <summary>Hochwertige Trennung über audio-separator (RoFormer/Demucs/Karaoke) für eine Spur.</summary>
    public async Task SeparateTrackHqAsync(StemTrackViewModel? track, string modelKey)
    {
        if (track is null || IsSeparating) return;
        var clip = track.Clips.FirstOrDefault();
        if (clip is null) return;
        var model = AdvSep.Models.FirstOrDefault(m => m.Key == modelKey);
        if (model is null) return;

        var path = clip.SourcePath;
        var offset = clip.TimelineOffsetSeconds;

        IsSeparating = true;
        SeparationStatus = $"HQ-Trennung ({model.Name}) – erster Lauf lädt Modell …";
        try
        {
            var prog = new Progress<string>(line =>
            {
                var m = System.Text.RegularExpressions.Regex.Match(line, @"(\d{1,3})%");
                SeparationStatus = m.Success ? $"HQ-Trennung … {m.Groups[1].Value} %" : $"HQ-Trennung ({model.Name}) …";
            });

            var stems = await AdvSep.SeparateAsync(path, model.ModelFilename, prog);
            if (stems.Count == 0)
            {
                _snackbar.Warning("Keine Stems", "Die Trennung hat nichts erzeugt.", 4);
                return;
            }

            var insertAt = Tracks.IndexOf(track) + 1;
            foreach (var s in stems)
            {
                var data = await _waveform.LoadAsync(s.FilePath, 4000);
                var dur = data.Duration.TotalSeconds;
                var t = StemTrackViewModel.ForFile(s.FilePath, $"{s.Name} ◂ {track.Name}", ColorForStemName(s.Name));
                t.LengthSeconds = dur;
                t.Clips.Add(new ClipViewModel
                {
                    Track = t, SourcePath = s.FilePath, SourceTotalSeconds = dur, SourcePeaks = data.Peaks,
                    TimelineOffsetSeconds = offset, SourceStartSeconds = 0, LengthSeconds = dur, Peaks = data.Peaks
                });
                Tracks.Insert(insertAt++, t);
            }

            OnPropertyChanged(nameof(HasTracks));
            RecomputeDuration();
            CommitClips();
            Commit("HQ-Stems getrennt");
            _snackbar.Success("Stems hinzugefügt", $"{stems.Count} Spuren ({model.Name}).", 4);
        }
        catch (Exception ex)
        {
            Audiola.Services.UiError.Show("HQ-Trennung fehlgeschlagen", ex.Message);
        }
        finally
        {
            IsSeparating = false;
            SeparationStatus = "";
        }
    }

    private static string ColorForStemName(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("vocal")) return "#FF6B6B";
        if (n.Contains("drum")) return "#FFB454";
        if (n.Contains("bass")) return "#5B8CFF";
        if (n.Contains("gitar") || n.Contains("guitar")) return "#54D6A0";
        if (n.Contains("klavier") || n.Contains("piano")) return "#6BD6FF";
        if (n.Contains("instrument")) return "#9B8CFF";
        return "#9B8CFF";
    }

    private static string StemName(Audiola.Models.StemKind k) => k switch
    {
        Audiola.Models.StemKind.Vocals => "Vocals",
        Audiola.Models.StemKind.Drums => "Drums",
        Audiola.Models.StemKind.Bass => "Bass",
        Audiola.Models.StemKind.Guitar => "Guitar",
        Audiola.Models.StemKind.Piano => "Piano",
        _ => "Other"
    };

    private static string StemColor(Audiola.Models.StemKind k) => k switch
    {
        Audiola.Models.StemKind.Vocals => "#FF6B6B",
        Audiola.Models.StemKind.Drums => "#FFB454",
        Audiola.Models.StemKind.Bass => "#5B8CFF",
        Audiola.Models.StemKind.Guitar => "#54D6A0",
        Audiola.Models.StemKind.Piano => "#6BD6FF",
        _ => "#9B8CFF"
    };
}
