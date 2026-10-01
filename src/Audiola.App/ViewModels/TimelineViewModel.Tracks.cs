using CommunityToolkit.Mvvm.Input;

namespace Audiola.ViewModels;

/// <summary>Spur-Zustände fürs Studio: ausblenden, sperren, „klingt gerade" und neue Spur per Drag.</summary>
public sealed partial class TimelineViewModel
{
    /// <summary>Die im Editor sichtbaren Spuren in Anzeigereihenfolge (für Y → Spur im View).</summary>
    public IReadOnlyList<StemTrackViewModel> VisibleTracks() => Tracks.Where(t => !t.IsHidden).ToList();

    public IReadOnlyList<StemTrackViewModel> HiddenTracks => Tracks.Where(t => t.IsHidden).ToList();
    public bool HasHiddenTracks => Tracks.Any(t => t.IsHidden);

    public string HiddenTracksText => HiddenTracks.Count switch
    {
        1 => "1 Spur ausgeblendet",
        var n => $"{n} Spuren ausgeblendet"
    };

    private List<StemTrackViewModel> AudibleHiddenTracks() => Tracks.Where(t => t.IsHidden && t.IsAudible).ToList();
    public bool HasAudibleHiddenTracks => Tracks.Any(t => t.IsHidden && t.IsAudible);

    public string AudibleHiddenTracksText => AudibleHiddenTracks() switch
    {
        [var one] => $"Die ausgeblendete Spur „{one.Name}“ wird mit abgespielt und exportiert.",
        var list => $"{list.Count} ausgeblendete Spuren werden mit abgespielt und exportiert: {string.Join(", ", list.Select(t => t.Name))}."
    };

    /// <summary>Hörbarkeit aller Spuren neu bestimmen (Solo einer Spur verdrängt die anderen) und Hinweise aktualisieren.</summary>
    private void RefreshTrackFlags()
    {
        var anySolo = Tracks.Any(t => t.IsSolo);
        foreach (var t in Tracks)
            t.IsAudible = t.IsEnabled && !t.IsMuted && (!anySolo || t.IsSolo);

        OnPropertyChanged(nameof(HiddenTracks));
        OnPropertyChanged(nameof(HasHiddenTracks));
        OnPropertyChanged(nameof(HiddenTracksText));
        OnPropertyChanged(nameof(HasAudibleHiddenTracks));
        OnPropertyChanged(nameof(AudibleHiddenTracksText));
    }

    [RelayCommand]
    private void HideTrack(StemTrackViewModel? track)
    {
        if (track is null) return;
        track.IsHidden = true;
        if (SelectedClip?.Track == track) { SelectedClip.IsSelected = false; SelectedClip = null; }
        if (SelectedTrack == track) SelectTrack(null);
        if (SelectionTrack == track) ClearSelection();
    }

    [RelayCommand]
    private void ShowTrack(StemTrackViewModel? track)
    {
        if (track is not null) track.IsHidden = false;
    }

    [RelayCommand]
    private void ShowAllTracks()
    {
        foreach (var t in Tracks) t.IsHidden = false;
    }

    /// <summary>Schaltet alle ausgeblendeten, aber hörbaren Spuren stumm.</summary>
    [RelayCommand]
    private void MuteHiddenTracks()
    {
        foreach (var t in AudibleHiddenTracks())
        {
            t.IsMuted = true;
            t.IsSolo = false;   // ein Solo würde sie sonst weiter hörbar halten
        }
    }

    /// <summary>Meldet einen Bearbeitungsversuch auf einer gesperrten Spur; true = abbrechen.</summary>
    private bool BlockedByLock(StemTrackViewModel? track)
    {
        if (track is not { IsLocked: true }) return false;
        _snackbar.Warning("Spur gesperrt",
            $"„{track.Name}“ ist gesperrt. Zum Bearbeiten erst das Schloss im Spurkopf lösen.", 4);
        return true;
    }

    /// <summary>
    /// Legt an <paramref name="insertIndex"/> (Index in <see cref="Tracks"/>) eine neue Spur an und
    /// verschiebt den Clip dorthin — das „Neue Spur einfügen"-Ziel beim Clip-Ziehen.
    /// </summary>
    public bool MoveClipToNewTrack(ClipViewModel clip, int insertIndex, double newOffsetSeconds)
    {
        if (BlockedByLock(clip.Track)) return false;
        var track = StemTrackViewModel.ForFile("", $"Spur {Tracks.Count + 1}", Palette[Tracks.Count % Palette.Length]);
        Tracks.Insert(Math.Clamp(insertIndex, 0, Tracks.Count), track);
        OnPropertyChanged(nameof(HasTracks));
        return MoveClipToTrack(clip, track, newOffsetSeconds);
    }
}
