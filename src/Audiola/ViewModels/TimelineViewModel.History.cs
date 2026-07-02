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
    private StudioSnapshot Capture() => new(
        Tracks.Select(t => new TrackSnap(
            t.Name, t.AccentColor, t.Model.FilePath, t.Volume, t.Pan,
            t.IsEnabled, t.IsMuted, t.IsSolo, t.StartOffsetSeconds, t.LengthSeconds, t.Peaks,
            t.Clips.Select(c => new ClipSnap(c.SourcePath, c.SourceTotalSeconds, c.SourcePeaks,
                c.TimelineOffsetSeconds, c.SourceStartSeconds, c.LengthSeconds, c.Peaks,
                c.GainDb, c.FadeInSeconds, c.FadeOutSeconds)).ToList())).ToList(),
        MasterVolume);

    /// <summary>Nimmt den aktuellen (bereits geänderten) Zustand als neuen Verlaufseintrag auf.</summary>
    public void Commit(string label)
    {
        // Redo-Zweig verwerfen, neuen Stand anhängen.
        if (_index < _states.Count - 1)
            _states.RemoveRange(_index + 1, _states.Count - _index - 1);
        _states.Add(new HistoryState(Capture(), label));
        if (_states.Count > MaxStates + 1) _states.RemoveAt(0);
        _index = _states.Count - 1;
        IsDirty = true;
        RebuildHistory();
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_index <= 0) return;
        _index--;
        Restore(_states[_index].Snap);
        IsDirty = true;
        RebuildHistory();
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (_index >= _states.Count - 1) return;
        _index++;
        Restore(_states[_index].Snap);
        IsDirty = true;
        RebuildHistory();
    }

    /// <summary>Springt direkt zu einem bestimmten Verlaufseintrag.</summary>
    [RelayCommand]
    private void JumpToHistory(HistoryEntryViewModel? entry)
    {
        if (entry is null || entry.Index < 0 || entry.Index >= _states.Count || entry.Index == _index) return;
        _index = entry.Index;
        Restore(_states[_index].Snap);
        IsDirty = true;
        RebuildHistory();
    }

    private void ResetHistory(string label)
    {
        _states.Clear();
        _states.Add(new HistoryState(Capture(), label));
        _index = 0;
        RebuildHistory();
    }

    private void RebuildHistory()
    {
        History.Clear();
        for (var i = 0; i < _states.Count; i++)
            History.Add(new HistoryEntryViewModel(i, _states[i].Label, i == _index, i > _index));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void Restore(StudioSnapshot snap)
    {
        _suppressDirty = true;
        Tracks.Clear();
        SelectedClip = null;
        foreach (var ts in snap.Tracks)
        {
            var t = StemTrackViewModel.ForFile(ts.FilePath, ts.Name, ts.AccentColor);
            t.Volume = ts.Volume; t.Pan = ts.Pan;
            t.IsEnabled = ts.IsEnabled; t.IsMuted = ts.IsMuted; t.IsSolo = ts.IsSolo;
            t.StartOffsetSeconds = ts.StartOffsetSeconds; t.LengthSeconds = ts.LengthSeconds; t.Peaks = ts.Peaks;
            foreach (var cs in ts.Clips)
                t.Clips.Add(new ClipViewModel
                {
                    Track = t,
                    SourcePath = cs.SourcePath,
                    SourceTotalSeconds = cs.SourceTotalSeconds,
                    SourcePeaks = cs.SourcePeaks,
                    TimelineOffsetSeconds = cs.TimelineOffsetSeconds,
                    SourceStartSeconds = cs.SourceStartSeconds,
                    LengthSeconds = cs.LengthSeconds,
                    Peaks = cs.Peaks,
                    GainDb = cs.GainDb,
                    FadeInSeconds = cs.FadeInSeconds,
                    FadeOutSeconds = cs.FadeOutSeconds
                });
            Tracks.Add(t);
        }
        MasterVolume = snap.MasterVolume;
        _suppressDirty = false;

        OnPropertyChanged(nameof(HasTracks));
        RecomputeDuration();
        UpdateContentWidth();
        CommitClips();
    }
}
