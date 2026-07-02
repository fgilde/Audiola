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

/// <summary>
/// Multitrack-Timeline: übernimmt die Stems als Spuren, zeigt sie entlang einer
/// gemeinsamen Zeitachse mit Zoom und mitlaufendem Playhead. Wiedergabe über die
/// gemeinsame <see cref="StemMixerEngine"/>; die globale Transportleiste steuert mit.
/// </summary>
public sealed partial class TimelineViewModel : ObservableObject
{
    private const double MinPps = 10;
    private const double MaxPps = 200;

    private readonly SessionState _session;
    private readonly StemMixerEngine _engine;
    private readonly IWaveformService _waveform;
    private readonly TransportViewModel _transport;
    private readonly IStemSeparationService _separation;
    private IAdvancedSeparationService? _advSep;
    public IAdvancedSeparationService AdvSep => _advSep ??= App.GetService<IAdvancedSeparationService>();
    private ExportService? _export;
    private ExportService Export => _export ??= App.GetService<ExportService>();
    private SongMetadata? _songMeta;
    private SongMetadata SongMeta => _songMeta ??= App.GetService<SongMetadata>();
    private readonly IVoiceChangeService _voiceChange;
    private readonly ILocalVoiceService _localVoice;
    private readonly ISettingsService _settings;
    private readonly ISnackbarService _snackbar;

    /// <summary>Registrierte Variations-Provider (z. B. „Studio-Effekte", „Internal", Python …).</summary>
    public IReadOnlyList<IAudioVariationProvider> VariationProviders { get; }

    /// <summary>Echtzeit-Spektrum (0..1 je Band) im Wiedergabe-Takt — für die Header-Visualisierung.</summary>
    public event EventHandler<float[]>? SpectrumUpdated;

    /// <summary>Verlauf-Panel ein-/ausblenden.</summary>
    [ObservableProperty] private bool _showHistory;

    public ObservableCollection<StemTrackViewModel> Tracks { get; } = [];

    [ObservableProperty] private double _pixelsPerSecond = 40;
    [ObservableProperty] private double _durationSeconds;
    [ObservableProperty] private Thickness _playheadMargin;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _snapEnabled = true;
    [ObservableProperty] private double _gridSeconds = 0.25;
    [ObservableProperty] private double _selectionStartSeconds;
    [ObservableProperty] private double _selectionEndSeconds;
    [ObservableProperty] private bool _loopEnabled;
    [ObservableProperty] private double _masterVolume = 1.0;
    [ObservableProperty] private bool _showMixer;

    /// <summary>Höhe einer Spur in Pixeln (für größere Wellenform-Darstellung).</summary>
    [ObservableProperty] private double _laneHeight = 108;

    /// <summary>Auswahl-Werkzeug: Ziehen in den Spuren markiert einen Bereich, statt Clips zu verschieben.</summary>
    [ObservableProperty] private bool _rangeSelectMode;

    partial void OnMasterVolumeChanged(double value) { _engine.MasterVolume = (float)value; MarkDirty(); }

    public bool HasSelection => SelectionEndSeconds > SelectionStartSeconds + 1e-6;
    public double SelectionLengthSeconds => Math.Max(0, SelectionEndSeconds - SelectionStartSeconds);

    partial void OnSelectionStartSecondsChanged(double value) => SelectionChanged();
    partial void OnSelectionEndSecondsChanged(double value) => SelectionChanged();
    partial void OnLoopEnabledChanged(bool value) => UpdateLoop();

    /// <summary>Spur, auf der der Auswahlbereich liegt (null = global, z. B. vom Zeit-Lineal).</summary>
    private StemTrackViewModel? _selectionTrack;
    public StemTrackViewModel? SelectionTrack
    {
        get => _selectionTrack;
        private set
        {
            _selectionTrack = value;
            foreach (var t in Tracks) t.IsSelectionTrack = ReferenceEquals(t, value);
            OnPropertyChanged(nameof(HasGlobalSelection));
        }
    }

    /// <summary>Auswahl ohne bestimmte Spur (über alle Spuren, z. B. fürs Loopen/Exportieren).</summary>
    public bool HasGlobalSelection => HasSelection && SelectionTrack is null;

    private void SelectionChanged()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(HasGlobalSelection));
        OnPropertyChanged(nameof(SelectionLengthSeconds));
        OnPropertyChanged(nameof(HasClipRegion));
        OnPropertyChanged(nameof(ClipEffectScope));
        OnPropertyChanged(nameof(CanCutRegion));
        CutRegionCommand.NotifyCanExecuteChanged();
        UpdateLoop();
        ExportRangeCommand.NotifyCanExecuteChanged();
    }

    private void UpdateLoop()
    {
        _engine.LoopEnabled = LoopEnabled && HasSelection;
        _engine.LoopStart = TimeSpan.FromSeconds(SelectionStartSeconds);
        _engine.LoopEnd = TimeSpan.FromSeconds(SelectionEndSeconds);
    }

    /// <summary>Setzt den Auswahlbereich global (z. B. vom Zeit-Lineal).</summary>
    public void SetSelection(double a, double b) => SetSelection(a, b, null);

    /// <summary>Setzt den Auswahlbereich für eine bestimmte Spur (Band nur dort).</summary>
    public void SetSelection(double a, double b, StemTrackViewModel? track)
    {
        SelectionTrack = track;
        SelectionStartSeconds = Math.Max(0, Math.Min(a, b));
        SelectionEndSeconds = Math.Max(0, Math.Max(a, b));
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectionTrack = null;
        SelectionStartSeconds = 0;
        SelectionEndSeconds = 0;
    }

    private bool CanExportRange => HasSelection;

    /// <summary>Liefert einen Vorschlags-Dateinamen aus den Song-Metadaten (Interpret – Titel) oder einem Fallback.</summary>
    private string DefaultExportName(string fallbackSuffix)
    {
        var m = SongMeta.ToMetadata();
        if (!string.IsNullOrWhiteSpace(m.Title))
            return string.IsNullOrWhiteSpace(m.Artist) ? m.Title! : $"{m.Artist} - {m.Title}";
        return $"audiola-{fallbackSuffix}";
    }

    public IReadOnlyList<double> GridOptions { get; } = [0.1, 0.25, 0.5, 1.0];

    /// <summary>Rastet Sekunden auf das Raster ein (falls aktiv) und klemmt auf ≥ 0.</summary>
    public double Snap(double seconds)
    {
        seconds = Math.Max(0, seconds);
        if (!SnapEnabled || GridSeconds <= 0) return seconds;
        return Math.Round(seconds / GridSeconds) * GridSeconds;
    }

    public double ContentWidth => Math.Max(0, DurationSeconds * PixelsPerSecond);
    public bool HasTracks => Tracks.Count > 0;

    public TimelineViewModel(
        SessionState session,
        StemMixerEngine engine,
        IWaveformService waveform,
        TransportViewModel transport,
        IStemSeparationService separation,
        IVoiceChangeService voiceChange,
        ILocalVoiceService localVoice,
        ISettingsService settings,
        IEnumerable<IAudioVariationProvider> variationProviders,
        ISnackbarService snackbar)
    {
        _session = session;
        _engine = engine;
        _waveform = waveform;
        _transport = transport;
        _separation = separation;
        _voiceChange = voiceChange;
        _localVoice = localVoice;
        _settings = settings;
        VariationProviders = variationProviders.ToList();
        _snackbar = snackbar;

        _engine.PositionChanged += (_, _) => UpdatePlayhead();
        _engine.StateChanged += (_, _) => IsPlaying = _engine.IsPlaying;
        _engine.SpectrumUpdated += (_, bands) => SpectrumUpdated?.Invoke(this, bands);

        _states.Add(new HistoryState(Capture(), "Start"));
        RebuildHistory();

        // „Geändert"-Erkennung: Struktur- und Eigenschaftsänderungen markieren das Projekt als dirty.
        Tracks.CollectionChanged += (_, e) =>
        {
            if (e.NewItems is not null)
                foreach (StemTrackViewModel t in e.NewItems) HookTrack(t);
            MarkDirty();
        };
    }

    // ---- „Geändert"-Status (für „Speichern beim Beenden?") ----

    private bool _suppressDirty;

    /// <summary>True, sobald seit dem letzten Speichern/Laden etwas geändert wurde.</summary>
    [ObservableProperty] private bool _isDirty;

    /// <summary>Pfad der aktuell geöffneten/gespeicherten Projektdatei (null = noch keine).</summary>
    [ObservableProperty] private string? _currentProjectPath;   // observable → Fenstertitel folgt

    private void MarkDirty() { if (!_suppressDirty) IsDirty = true; }

    private void HookTrack(StemTrackViewModel t)
    {
        t.PropertyChanged += (_, e) =>
        {
            // Pegel-Updates (Wiedergabe) und Auswahl-Markierungen sind keine Bearbeitung.
            if (e.PropertyName is not (nameof(StemTrackViewModel.Level)
                or nameof(StemTrackViewModel.IsSelectionTrack)
                or nameof(StemTrackViewModel.IsSelectedTrack)))
                MarkDirty();
        };
        t.Clips.CollectionChanged += (_, e) =>
        {
            if (e.NewItems is not null)
                foreach (ClipViewModel c in e.NewItems) HookClip(c);
            MarkDirty();
        };
        foreach (var c in t.Clips) HookClip(c);
    }

    private void HookClip(ClipViewModel c) =>
        c.PropertyChanged += (_, e) =>
        {
            // Auswahl ist keine Bearbeitung.
            if (e.PropertyName != nameof(ClipViewModel.IsSelected)) MarkDirty();
        };

    private static readonly string[] Palette =
        ["#5B8CFF", "#FF6B6B", "#FFB454", "#9B8CFF", "#54D6A0", "#FF8AD8", "#6BD6FF", "#D6C054"];

    /// <summary>Beim Anzeigen: bestehendes Arrangement behalten; sonst Stems einmalig übernehmen.</summary>
    public void OnActivated()
    {
        if (!HasTracks && _session.CurrentStemSet is not null)
        {
            ImportStems();
        }
        else if (HasTracks)
        {
            // Läuft die Wiedergabe, NICHT neu laden — sonst stoppt sie bei jeder Rückkehr
            // ins Studio und die Zeit springt auf 0. Spur-Änderungen laden die Engine ohnehin
            // selbst neu (CommitClips); im Pause-Zustand bleibt die Position erhalten.
            if (!_engine.IsPlaying)
            {
                var pos = _engine.Position;
                _engine.Load(Tracks);
                if (pos > TimeSpan.Zero && pos < _engine.Duration) _engine.Position = pos;
            }
            DurationSeconds = _engine.Duration.TotalSeconds;
        }

        if (HasTracks) _transport.SetMode(TransportMode.StemMix);
        UpdateContentWidth();
        UpdatePlayhead();
    }

    public void OnDeactivated() { /* Transport bleibt auf dem Studio-Mix (Auto-Follow) */ }

    /// <summary>Übernimmt die aktuellen Stems als zusätzliche Spuren.</summary>
    [RelayCommand]
    private void ImportStems()
    {
        if (_session.CurrentStemSet is not { } set) return;

        var added = new List<StemTrackViewModel>();
        foreach (var stem in set.Stems)
        {
            var vm = new StemTrackViewModel(stem);
            Tracks.Add(vm);
            added.Add(vm);
        }
        OnPropertyChanged(nameof(HasTracks));
        _ = LoadPeaksAsync(added);
        if (HasTracks) _transport.SetMode(TransportMode.StemMix);
        if (added.Count > 0) Commit("Stems übernommen");
    }

    /// <summary>Öffnet einen Dialog und fügt gewählte Audiodateien als neue Spuren hinzu.</summary>
    [RelayCommand]
    private async Task AddAudioAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Audiodatei(en) als Spur hinzufügen",
            Multiselect = true,
            Filter = "Audiodateien|*.wav;*.mp3;*.flac;*.aiff;*.m4a;*.ogg|Alle Dateien|*.*"
        };
        if (dialog.ShowDialog() != true) return;
        foreach (var f in dialog.FileNames)
            await AddAudioFileAsync(f, -1, 0);
    }

    /// <summary>
    /// Fügt eine Audiodatei als Clip hinzu — entweder in eine bestehende Spur
    /// (trackIndex ≥ 0) oder als neue Spur.
    /// </summary>
    public async Task AddAudioFileAsync(string path, int trackIndex, double offsetSeconds)
    {
        AudioTrackData t;
        try { t = await LoadClipDataAsync(path); }
        catch { return; }

        StemTrackViewModel track;
        if (trackIndex >= 0 && trackIndex < Tracks.Count)
        {
            track = Tracks[trackIndex];
        }
        else
        {
            track = StemTrackViewModel.ForFile(path,
                System.IO.Path.GetFileNameWithoutExtension(path),
                Palette[Tracks.Count % Palette.Length]);
            Tracks.Add(track);
            OnPropertyChanged(nameof(HasTracks));
        }

        track.Clips.Add(new ClipViewModel
        {
            Track = track,
            SourcePath = path,
            SourceTotalSeconds = t.Seconds,
            SourcePeaks = t.Peaks,
            TimelineOffsetSeconds = Snap(offsetSeconds),
            SourceStartSeconds = 0,
            LengthSeconds = t.Seconds,
            Peaks = t.Peaks
        });

        RecomputeDuration();
        CommitClips();
        if (HasTracks) _transport.SetMode(TransportMode.StemMix);
        Commit("Audio hinzugefügt");
    }

    private readonly record struct AudioTrackData(float[] Peaks, double Seconds);

    private async Task<AudioTrackData> LoadClipDataAsync(string path)
    {
        var t = await _waveform.LoadAsync(path, 4000);
        return new AudioTrackData(t.Peaks, t.Duration.TotalSeconds);
    }

    private async Task LoadPeaksAsync(IReadOnlyList<StemTrackViewModel> tracks)
    {
        foreach (var vm in tracks)
        {
            try
            {
                // Mehr Buckets für die breitere Timeline-Darstellung.
                var t = await _waveform.LoadAsync(vm.Model.FilePath, 4000);
                vm.Peaks = t.Peaks;
                vm.LengthSeconds = t.Duration.TotalSeconds;

                // Anfangs ein Clip über den ganzen Stem.
                vm.Clips.Clear();
                vm.Clips.Add(new ClipViewModel
                {
                    Track = vm,
                    SourcePath = vm.Model.FilePath,
                    SourceTotalSeconds = vm.LengthSeconds,
                    SourcePeaks = vm.Peaks,
                    TimelineOffsetSeconds = 0,
                    SourceStartSeconds = 0,
                    LengthSeconds = vm.LengthSeconds,
                    Peaks = vm.Peaks
                });
            }
            catch { /* Wellenform optional */ }
        }
        RecomputeDuration();
        if (HasTracks) _engine.Load(Tracks); // auf Clip-Modus umstellen
    }

    private void RecomputeDuration()
    {
        double max = 0;
        foreach (var vm in Tracks)
        {
            if (vm.Clips.Count == 0)
                max = Math.Max(max, vm.StartOffsetSeconds + vm.LengthSeconds);
            else
                foreach (var clip in vm.Clips)
                    max = Math.Max(max, clip.EndSeconds);
        }
        if (max > 0) DurationSeconds = max;
    }

    [ObservableProperty] private ClipViewModel? _selectedClip;

    public bool HasSelectedClip => SelectedClip is not null;
    partial void OnSelectedClipChanged(ClipViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedClip));
        OnPropertyChanged(nameof(HasClipRegion));
        OnPropertyChanged(nameof(ClipEffectScope));
        OnPropertyChanged(nameof(CanCutRegion));
        CutRegionCommand.NotifyCanExecuteChanged();
    }

    public void SelectClip(ClipViewModel clip)
    {
        foreach (var t in Tracks)
            foreach (var c in t.Clips)
                c.IsSelected = ReferenceEquals(c, clip);
        SelectedClip = clip;
        SelectTrack(clip?.Track);
    }

    /// <summary>Aktuell ausgewählte Spur (für spurbezogene Werkzeuge + hervorgehobene Anzeige).</summary>
    [ObservableProperty] private StemTrackViewModel? _selectedTrack;

    public bool HasSelectedTrack => SelectedTrack is not null;
    partial void OnSelectedTrackChanged(StemTrackViewModel? value)
        => OnPropertyChanged(nameof(HasSelectedTrack));

    /// <summary>Markiert eine Spur als ausgewählt (null = keine).</summary>
    public void SelectTrack(StemTrackViewModel? track)
    {
        foreach (var t in Tracks) t.IsSelectedTrack = ReferenceEquals(t, track);
        SelectedTrack = track;
    }

    /// <summary>Während des Ziehens: Clip-Offset visuell setzen (ohne Engine-Reload).</summary>
    public void SetClipOffset(ClipViewModel clip, double seconds)
    {
        clip.TimelineOffsetSeconds = Snap(seconds);
        RecomputeDuration();
    }

    /// <summary>
    /// Verschiebt einen Clip auf eine andere Spur (und an eine neue Timeline-Position).
    /// Da <see cref="ClipViewModel.Track"/> init-only ist, wird der Clip in der Zielspur neu erzeugt.
    /// </summary>
    public void MoveClipToTrack(ClipViewModel clip, int targetTrackIndex, double newOffsetSeconds)
    {
        if (targetTrackIndex < 0 || targetTrackIndex >= Tracks.Count) return;
        var target = Tracks[targetTrackIndex];
        var offset = Snap(Math.Max(0, newOffsetSeconds));

        if (clip.Track == target)
        {
            clip.TimelineOffsetSeconds = offset;
            RecomputeDuration();
            CommitClips();
            return;
        }

        var replacement = new ClipViewModel
        {
            Track = target,
            SourcePath = clip.SourcePath,
            SourceTotalSeconds = clip.SourceTotalSeconds,
            SourcePeaks = clip.SourcePeaks,
            TimelineOffsetSeconds = offset,
            SourceStartSeconds = clip.SourceStartSeconds,
            LengthSeconds = clip.LengthSeconds,
            Peaks = clip.Peaks,
            GainDb = clip.GainDb,
            FadeInSeconds = clip.FadeInSeconds,
            FadeOutSeconds = clip.FadeOutSeconds
        };
        clip.Track.Clips.Remove(clip);
        target.Clips.Add(replacement);
        SelectedClip = replacement;
        replacement.IsSelected = true;
        RecomputeDuration();
        CommitClips();
    }

    [RelayCommand]
    private void SplitAtPlayhead()
    {
        var clip = SelectedClip;
        if (clip is null) return;

        var t = _engine.Position.TotalSeconds;
        if (t <= clip.TimelineOffsetSeconds + 0.02 || t >= clip.EndSeconds - 0.02) return;

        var track = clip.Track;
        var rel = t - clip.TimelineOffsetSeconds; // Sekunden in den Clip hinein
        var srcTotal = clip.SourceTotalSeconds <= 0 ? clip.LengthSeconds : clip.SourceTotalSeconds;

        var fStart = clip.SourceStartSeconds / srcTotal;
        var fSplit = (clip.SourceStartSeconds + rel) / srcTotal;
        var fEnd = (clip.SourceStartSeconds + clip.LengthSeconds) / srcTotal;

        var left = new ClipViewModel
        {
            Track = track,
            SourcePath = clip.SourcePath,
            SourceTotalSeconds = clip.SourceTotalSeconds,
            SourcePeaks = clip.SourcePeaks,
            TimelineOffsetSeconds = clip.TimelineOffsetSeconds,
            SourceStartSeconds = clip.SourceStartSeconds,
            LengthSeconds = rel,
            Peaks = SlicePeaks(clip.SourcePeaks, fStart, fSplit)
        };
        var right = new ClipViewModel
        {
            Track = track,
            SourcePath = clip.SourcePath,
            SourceTotalSeconds = clip.SourceTotalSeconds,
            SourcePeaks = clip.SourcePeaks,
            TimelineOffsetSeconds = clip.TimelineOffsetSeconds + rel,
            SourceStartSeconds = clip.SourceStartSeconds + rel,
            LengthSeconds = clip.LengthSeconds - rel,
            Peaks = SlicePeaks(clip.SourcePeaks, fSplit, fEnd)
        };

        var idx = track.Clips.IndexOf(clip);
        track.Clips.RemoveAt(idx);
        track.Clips.Insert(idx, right);
        track.Clips.Insert(idx, left);

        SelectedClip = null;
        CommitClips();
        Commit("Clip geteilt");
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedClip is null) return;
        SelectedClip.Track.Clips.Remove(SelectedClip);
        SelectedClip = null;
        CommitClips();
        Commit("Clip gelöscht");
    }

    /// <summary>True, wenn der markierte Bereich einen Teil des gewählten Clips abdeckt.</summary>
    public bool CanCutRegion => SelectedClip is { } c && ClipRegionSeconds(c) is not null;

    /// <summary>Schneidet den markierten Bereich aus dem gewählten Clip heraus (es bleibt eine Lücke).</summary>
    [RelayCommand(CanExecute = nameof(CanCutRegion))]
    private void CutRegion()
    {
        var clip = SelectedClip;
        if (clip is null || ClipRegionSeconds(clip) is not { } region) return;

        var track = clip.Track;
        var srcTotal = clip.SourceTotalSeconds <= 0 ? clip.LengthSeconds : clip.SourceTotalSeconds;
        var idx = track.Clips.IndexOf(clip);
        if (idx < 0) return;

        var aSec = region.aSec; // clip-relative Sekunden
        var bSec = region.bSec;

        var pieces = new List<ClipViewModel>();
        // Linkes Reststück [0, aSec)
        if (aSec > MinClipSeconds)
        {
            var f0 = clip.SourceStartSeconds / srcTotal;
            var f1 = (clip.SourceStartSeconds + aSec) / srcTotal;
            pieces.Add(new ClipViewModel
            {
                Track = track,
                SourcePath = clip.SourcePath,
                SourceTotalSeconds = clip.SourceTotalSeconds,
                SourcePeaks = clip.SourcePeaks,
                TimelineOffsetSeconds = clip.TimelineOffsetSeconds,
                SourceStartSeconds = clip.SourceStartSeconds,
                LengthSeconds = aSec,
                Peaks = SlicePeaks(clip.SourcePeaks, f0, f1),
                GainDb = clip.GainDb,
                FadeInSeconds = clip.FadeInSeconds
            });
        }
        // Rechtes Reststück [bSec, len)
        if (clip.LengthSeconds - bSec > MinClipSeconds)
        {
            var f0 = (clip.SourceStartSeconds + bSec) / srcTotal;
            var f1 = (clip.SourceStartSeconds + clip.LengthSeconds) / srcTotal;
            pieces.Add(new ClipViewModel
            {
                Track = track,
                SourcePath = clip.SourcePath,
                SourceTotalSeconds = clip.SourceTotalSeconds,
                SourcePeaks = clip.SourcePeaks,
                TimelineOffsetSeconds = clip.TimelineOffsetSeconds + bSec,
                SourceStartSeconds = clip.SourceStartSeconds + bSec,
                LengthSeconds = clip.LengthSeconds - bSec,
                Peaks = SlicePeaks(clip.SourcePeaks, f0, f1),
                FadeOutSeconds = clip.FadeOutSeconds
            });
        }

        track.Clips.RemoveAt(idx);
        for (var i = 0; i < pieces.Count; i++)
            track.Clips.Insert(idx + i, pieces[i]);

        SelectedClip = pieces.FirstOrDefault();
        if (SelectedClip is not null) SelectedClip.IsSelected = true;
        ClearSelection();
        RecomputeDuration();
        CommitClips();
        Commit("Bereich ausgeschnitten");
    }

    [ObservableProperty] private bool _isSeparating;
    [ObservableProperty] private string _separationStatus = "";

    /// <summary>RMS-Schwelle (dBFS), ab der ein Stem als „vorhanden" gilt.</summary>
    private const double StemPresenceDb = -50.0;

    [RelayCommand]
    private void DeleteTrack(StemTrackViewModel? track)
    {
        if (track is null) return;
        Tracks.Remove(track);
        OnPropertyChanged(nameof(HasTracks));
        RecomputeDuration();
        CommitClips();
        Commit("Spur gelöscht");
    }

    /// <summary>Dupliziert eine Spur samt Clips und Einstellungen (direkt darunter eingefügt).</summary>
    [RelayCommand]
    private void DuplicateTrack(StemTrackViewModel? track)
    {
        if (track is null) return;

        var copy = StemTrackViewModel.ForFile(track.Model.FilePath, track.Name + " (Kopie)", track.AccentColor);
        copy.Volume = track.Volume;
        copy.Pan = track.Pan;
        copy.IsEnabled = track.IsEnabled;
        copy.IsMuted = track.IsMuted;
        copy.IsSolo = track.IsSolo;
        copy.Lrc = track.Lrc;
        copy.StartOffsetSeconds = track.StartOffsetSeconds;
        copy.LengthSeconds = track.LengthSeconds;
        copy.Peaks = track.Peaks;

        foreach (var c in track.Clips)
            copy.Clips.Add(new ClipViewModel
            {
                Track = copy,
                SourcePath = c.SourcePath,
                SourceTotalSeconds = c.SourceTotalSeconds,
                SourcePeaks = c.SourcePeaks,
                TimelineOffsetSeconds = c.TimelineOffsetSeconds,
                SourceStartSeconds = c.SourceStartSeconds,
                LengthSeconds = c.LengthSeconds,
                Peaks = c.Peaks,
                GainDb = c.GainDb,
                FadeInSeconds = c.FadeInSeconds,
                FadeOutSeconds = c.FadeOutSeconds
            });

        var idx = Tracks.IndexOf(track);
        Tracks.Insert(idx < 0 ? Tracks.Count : idx + 1, copy);
        OnPropertyChanged(nameof(HasTracks));
        RecomputeDuration();
        CommitClips();
        if (HasTracks) _transport.SetMode(TransportMode.StemMix);
        Commit("Spur dupliziert");
    }

    /// <summary>Öffnet den „Spur mastern“-Dialog (EQ → Kompressor → LUFS) für die gewählte Spur.</summary>
    [RelayCommand]
    private void MasterTrack(StemTrackViewModel? track)
    {
        if (track is null) return;
        var dlg = new Audiola.Views.Dialogs.TrackMasteringDialog(App.GetService<MasteringViewModel>(), track)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        dlg.ShowDialog();
    }

    /// <summary>Öffnet das Einsing-Studio (Karaoke: Backing + Mikro, Ton-Feedback, Übernahme als Spur).</summary>
    [RelayCommand]
    private void OpenSingAlong()
    {
        if (!HasTracks)
        {
            _snackbar.Warning("Nichts zum Einsingen", "Lade zuerst einen Song oder Spuren ins Studio.");
            return;
        }
        var win = new Audiola.Views.Dialogs.SingAlongWindow(App.GetService<SingAlongViewModel>())
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        win.Show();   // eigenes Fenster, nicht-modal – kann neben dem Studio liegen
    }

    /// <summary>Rendert eine einzelne Spur (mit allen Clips/Pegeln) als interleaved Stereo + Samplerate.</summary>
    public Task<(float[] Samples, int SampleRate)> RenderTrackAsync(StemTrackViewModel track)
    {
        var end = track.Clips.Count > 0 ? track.Clips.Max(c => c.EndSeconds) : track.LengthSeconds;
        var single = new List<StemTrackViewModel> { track };
        return Task.Run(() => _engine.RenderRange(single, TimeSpan.Zero, TimeSpan.FromSeconds(Math.Max(0.1, end))));
    }

    /// <summary>
    /// Ersetzt den gesamten Inhalt einer Spur durch eine Audiodatei (z. B. gemastert) — als ein
    /// Clip ab Position 0. Flacht mehrere Clips zu einem zusammen. Mit Undo.
    /// </summary>
    public async Task ApplyProcessedTrackAsync(StemTrackViewModel track, string wavPath)
    {
        var data = await LoadClipDataAsync(wavPath);
        track.Clips.Clear();
        track.Clips.Add(new ClipViewModel
        {
            Track = track,
            SourcePath = wavPath,
            SourceTotalSeconds = data.Seconds,
            SourcePeaks = data.Peaks,
            TimelineOffsetSeconds = 0,
            SourceStartSeconds = 0,
            LengthSeconds = data.Seconds,
            Peaks = data.Peaks
        });
        RecomputeDuration();
        CommitClips();
        if (HasTracks) _transport.SetMode(TransportMode.StemMix);
        Commit("Spur gemastert");
    }

    private const double MinClipSeconds = 0.05;

    /// <summary>Linke Clip-Kante auf eine Timeline-Zeit ziehen (Offset + Quellstart + Länge).</summary>
    public void SetClipLeftEdge(ClipViewModel clip, double timelineSeconds)
    {
        var total = clip.SourceTotalSeconds <= 0 ? clip.LengthSeconds : clip.SourceTotalSeconds;
        var newStart = Snap(timelineSeconds);
        var delta = newStart - clip.TimelineOffsetSeconds;
        var newSrcStart = clip.SourceStartSeconds + delta;
        var newLen = clip.LengthSeconds - delta;

        if (newSrcStart < 0) { var fix = -newSrcStart; newSrcStart = 0; newStart += fix; newLen -= fix; }
        if (newLen < MinClipSeconds) return;

        clip.TimelineOffsetSeconds = newStart;
        clip.SourceStartSeconds = newSrcStart;
        clip.LengthSeconds = newLen;
        Reslice(clip, total);
        RecomputeDuration();
    }

    /// <summary>Rechte Clip-Kante auf eine Timeline-Zeit ziehen (Länge).</summary>
    public void SetClipRightEdge(ClipViewModel clip, double timelineSeconds)
    {
        var total = clip.SourceTotalSeconds <= 0 ? clip.LengthSeconds : clip.SourceTotalSeconds;
        var newEnd = Snap(timelineSeconds);
        var newLen = Math.Min(newEnd - clip.TimelineOffsetSeconds, total - clip.SourceStartSeconds);
        if (newLen < MinClipSeconds) return;

        clip.LengthSeconds = newLen;
        Reslice(clip, total);
        RecomputeDuration();
    }

    /// <summary>
    /// Vorschau beim Dehnen: ändert nur die Timeline-Länge (und bei der linken Kante den Offset),
    /// ohne die Quelle zu trimmen — die Wellenform wird dadurch optisch gestaucht/gedehnt.
    /// Das eigentliche Time-Stretching passiert beim Loslassen (<see cref="StretchClipToLengthAsync"/>).
    /// </summary>
    public void SetClipStretchEdge(ClipViewModel clip, double timelineSeconds, bool fromLeft, double anchorOffset, double anchorLen)
    {
        if (fromLeft)
        {
            var rightEdge = anchorOffset + anchorLen;
            var newStart = Math.Clamp(Snap(timelineSeconds), 0, rightEdge - MinClipSeconds);
            clip.TimelineOffsetSeconds = newStart;
            clip.LengthSeconds = rightEdge - newStart;
        }
        else
        {
            clip.LengthSeconds = Math.Max(MinClipSeconds, Snap(timelineSeconds) - clip.TimelineOffsetSeconds);
        }
        RecomputeDuration();
    }

    /// <summary>
    /// Dehnt/staucht das Clip-Audio per Time-Stretch (SoundTouch, Tonhöhe bleibt) auf die neue Länge
    /// und backt es als neue Clip-Quelle. <paramref name="origSrcLen"/> ist die ursprüngliche
    /// Quell-Dauer (vor dem Ziehen), <paramref name="newLen"/> die Ziel-Timeline-Länge.
    /// </summary>
    public async Task StretchClipToLengthAsync(ClipViewModel clip, double newLen, double origSrcStart, double origSrcLen)
    {
        var stretch = origSrcLen > 0.001 ? newLen / origSrcLen : 1.0;
        // Kaum verändert → kein Re-Encode, nur committen.
        if (origSrcLen < 0.02 || newLen < MinClipSeconds || Math.Abs(stretch - 1.0) < 0.01)
        {
            CommitClips();
            Commit("Clip gedehnt");
            return;
        }

        var path = clip.SourcePath;
        try
        {
            var (buf, sr) = await Task.Run(() =>
            {
                var (all, rate) = AudioProcessingHelper.ReadStereo(path);
                var startS = (int)Math.Clamp((long)(origSrcStart * rate) * 2, 0, all.Length);
                var lenS = (int)Math.Clamp((long)(origSrcLen * rate) * 2, 0, all.Length - startS);
                var seg = new float[lenS];
                Array.Copy(all, startS, seg, 0, lenS);

                var stretched = new SoundTouchProfileProvider(new FloatArraySampleProvider(seg, rate, 2), 0, stretch);
                return (SampleProviderRenderer.RenderAllSamples(stretched), rate);
            });
            ReplaceClipFromBuffer(clip, buf, sr);   // setzt Länge = neue Audio-Dauer, committet
        }
        catch (Exception ex)
        {
            Audiola.Services.UiError.Show("Dehnen fehlgeschlagen", ex.Message);
        }
    }

    private static void Reslice(ClipViewModel clip, double total)
    {
        if (total <= 0) return;
        var fStart = clip.SourceStartSeconds / total;
        var fEnd = (clip.SourceStartSeconds + clip.LengthSeconds) / total;
        clip.Peaks = SlicePeaks(clip.SourcePeaks, fStart, fEnd);
    }

    // ---- Clip-Bearbeitung (backen destruktiv in den Clip) ----
    // Einzel-Effekte (Hall/Echo/…) gibt es als Variationen im „Studio-Effekte"-Provider
    // (Button/Kontextmenü „Variationen…"); ApplyClipEffect entfällt dadurch.

    /// <summary>
    /// Bereich (in Clip-relativen Sekunden), auf den Clip-Operationen wirken: Schnittmenge
    /// der Timeline-Auswahl mit dem Clip. null = kein/keine Überlappung → ganzer Clip.
    /// </summary>
    private (double aSec, double bSec)? ClipRegionSeconds(ClipViewModel clip)
    {
        if (!HasSelection) return null;
        var clipStart = clip.TimelineOffsetSeconds;
        var clipEnd = clipStart + clip.LengthSeconds;
        var rStart = Math.Max(clipStart, SelectionStartSeconds);
        var rEnd = Math.Min(clipEnd, SelectionEndSeconds);
        if (rEnd <= rStart + 1e-6) return null;
        return (rStart - clipStart, rEnd - clipStart);
    }

    /// <summary>True, wenn die Auswahl einen Teil des gewählten Clips abdeckt (Effekte wirken nur dort).</summary>
    public bool HasClipRegion => SelectedClip is { } c && ClipRegionSeconds(c) is not null;

    /// <summary>Beschriftung für den Effekt-Bereich im Inspektor.</summary>
    public string ClipEffectScope => HasClipRegion ? "Effekte (auf Auswahl)" : "Effekte (ganzer Clip)";

    [ObservableProperty] private bool _isVoiceChanging;

    [ObservableProperty] private bool _isTranscribing;

    /// <summary>True, wenn ElevenLabs als Lyrics-Engine zur Verfügung steht (API-Key hinterlegt).</summary>
    public bool ElevenLabsAvailable => _voiceChange.HasApiKey;

    /// <summary>Ersetzt die Quelle eines Clips durch einen bearbeiteten Puffer (Editor-Bake / Voice-Change).</summary>
    public void ReplaceClipFromBuffer(ClipViewModel clip, float[] samples, int sampleRate)
    {
        var temp = TempDir.File("clipfx", ".wav", "edit");
        AudioEdits.WriteWav(temp, samples, sampleRate);
        var lenSec = (double)(samples.Length / 2) / sampleRate;
        ReplaceSelectedClipSource(clip, temp, samples, lenSec);
        CommitClips();
        Commit("Clip ersetzt");
    }

    private void ReplaceSelectedClipSource(ClipViewModel clip, string temp, float[] outBuf, double lenSec)
    {
        var r = ReplaceClipSource(clip, temp, outBuf, lenSec);
        if (r is not null) { SelectedClip = r; r.IsSelected = true; }
    }

    /// <summary>Ersetzt einen beliebigen Clip durch gebackenes Material; behält die Auswahl, falls betroffen.</summary>
    private ClipViewModel? ReplaceClipSource(ClipViewModel clip, string temp, float[] outBuf, double lenSec)
    {
        var track = clip.Track;
        var idx = track.Clips.IndexOf(clip);
        if (idx < 0) return null;

        var peaks = AudioEdits.ComputePeaks(outBuf);
        var replacement = new ClipViewModel
        {
            Track = track,
            SourcePath = temp,
            SourceTotalSeconds = lenSec,
            SourcePeaks = peaks,
            TimelineOffsetSeconds = clip.TimelineOffsetSeconds,
            SourceStartSeconds = 0,
            LengthSeconds = lenSec,
            Peaks = peaks,
            GainDb = clip.GainDb,
            FadeInSeconds = clip.FadeInSeconds,
            FadeOutSeconds = clip.FadeOutSeconds
        };
        track.Clips[idx] = replacement;
        if (ReferenceEquals(SelectedClip, clip)) { SelectedClip = replacement; replacement.IsSelected = true; }
        return replacement;
    }

    /// <summary>
    /// Wendet die gewählten Variationen eines Providers nacheinander auf die angegebenen Clips an
    /// und backt das Ergebnis in die jeweiligen Clips (für Spur- oder Gesamt-Audio-Bearbeitung).
    /// </summary>
    public async Task ApplyVariationsAsync(IAudioVariationProvider provider, IReadOnlyList<string> variationIds, IReadOnlyList<ClipViewModel> clips)
    {
        if (provider is null || variationIds.Count == 0) return;
        var targets = clips.Where(c => !string.IsNullOrEmpty(c.SourcePath)).ToList();
        if (targets.Count == 0) return;

        try
        {
            foreach (var clip in targets)
            {
                var srcStart = clip.SourceStartSeconds;
                var len = clip.LengthSeconds;
                var region = ClipRegionSeconds(clip); // markierter Bereich (∩ Clip) oder null = ganzer Clip

                var prep = await Task.Run(() =>
                {
                    var (all, rate) = AudioProcessingHelper.ReadStereo(clip.SourcePath);
                    var startS = (int)Math.Clamp((long)(srcStart * rate) * 2, 0, all.Length);
                    var lenS = (int)Math.Clamp((long)(len * rate) * 2, 0, all.Length - startS);
                    var seg = new float[lenS];
                    Array.Copy(all, startS, seg, 0, lenS);

                    var frames = lenS / 2;
                    var a = region is null ? 0 : (int)Math.Clamp((long)(region.Value.aSec * rate), 0, frames);
                    var b = region is null ? frames : (int)Math.Clamp((long)(region.Value.bSec * rate), 0, frames);
                    return (seg, rate, aS: a * 2, bS: b * 2, whole: a == 0 && b == frames);
                });

                // Nur den (ggf. markierten) Bereich durch die Variationskette schicken …
                var sub = prep.whole ? prep.seg : prep.seg[prep.aS..prep.bS];
                foreach (var id in variationIds)
                    sub = await provider.ApplyAsync(id, sub, prep.rate);

                // … und wieder einsetzen (Rest des Clips bleibt unverändert).
                var newSeg = prep.whole ? sub : AudioProcessingHelper.SpliceStereo(prep.seg, prep.aS, prep.bS, sub);
                var sr = prep.rate;

                var temp = await Task.Run(() =>
                {
                    var t = TempDir.File("clipfx", ".wav", "var");
                    AudioEdits.WriteWav(t, newSeg, sr);
                    return t;
                });

                ReplaceClipSource(clip, temp, newSeg, (double)(newSeg.Length / 2) / sr);
            }

            CommitClips();
            Commit($"Variationen ({provider.Name})");
            _snackbar.Success("Variationen angewendet",
                $"{provider.Name}: {variationIds.Count} Variation(en) auf {targets.Count} Clip(s).");
        }
        catch (Exception ex)
        {
            _snackbar.Error("Variation fehlgeschlagen", ex.Message, 6);
        }
    }

    private static float[] SlicePeaks(float[] peaks, double startFrac, double endFrac)
    {
        if (peaks.Length < 2) return peaks;
        var buckets = peaks.Length / 2;
        var i0 = Math.Clamp((int)(startFrac * buckets), 0, buckets);
        var i1 = Math.Clamp((int)(endFrac * buckets), i0, buckets);
        var res = new float[(i1 - i0) * 2];
        Array.Copy(peaks, i0 * 2, res, 0, res.Length);
        return res;
    }

    // ---- Projekt speichern/laden (.audiola) ----

    /// <summary>Baut den serialisierbaren Projektzustand aus den aktuellen Spuren/Clips.</summary>
    /// <summary>Schließt das Projekt: leert die Spuren und setzt den Studio-Zustand zurück.</summary>
    public void CloseProject()
    {
        _suppressDirty = true;
        Tracks.Clear();
        SelectedClip = null;
        SelectTrack(null);
        _engine.Load(Tracks);   // Load([]) entlädt sauber
        DurationSeconds = 0;
        CurrentProjectPath = null;
        OnPropertyChanged(nameof(HasTracks));
        UpdateContentWidth();
        UpdatePlayhead();
        _suppressDirty = false;
        IsDirty = false;
    }

    public Audiola.Models.ProjectDto BuildProjectDto()
    {
        var dto = new Audiola.Models.ProjectDto
        {
            MasterVolume = MasterVolume,
            PixelsPerSecond = PixelsPerSecond,
            SnapEnabled = SnapEnabled,
            GridSeconds = GridSeconds,
            SelectedTrackIndex = SelectedClip is null ? -1 : Tracks.IndexOf(SelectedClip.Track)
        };

        foreach (var t in Tracks)
        {
            var td = new Audiola.Models.ProjectTrackDto
            {
                Name = t.Name,
                ColorHex = t.AccentColor,
                Volume = t.Volume,
                Pan = t.Pan,
                IsEnabled = t.IsEnabled,
                IsMuted = t.IsMuted,
                IsSolo = t.IsSolo,
                Lrc = t.Lrc
            };

            if (t.Clips.Count == 0 && !string.IsNullOrEmpty(t.Model.FilePath))
            {
                td.Clips.Add(new Audiola.Models.ProjectClipDto
                {
                    Media = t.Model.FilePath,
                    TimelineOffsetSeconds = t.StartOffsetSeconds,
                    SourceStartSeconds = 0,
                    LengthSeconds = t.LengthSeconds,
                    SourceTotalSeconds = t.LengthSeconds
                });
            }
            else
            {
                foreach (var c in t.Clips)
                    td.Clips.Add(new Audiola.Models.ProjectClipDto
                    {
                        Media = string.IsNullOrEmpty(c.SourcePath) ? t.Model.FilePath : c.SourcePath,
                        SourceTotalSeconds = c.SourceTotalSeconds,
                        TimelineOffsetSeconds = c.TimelineOffsetSeconds,
                        SourceStartSeconds = c.SourceStartSeconds,
                        LengthSeconds = c.LengthSeconds,
                        GainDb = c.GainDb,
                        FadeInSeconds = c.FadeInSeconds,
                        FadeOutSeconds = c.FadeOutSeconds
                    });
            }

            dto.Tracks.Add(td);
        }

        return dto;
    }

    /// <summary>Stellt das Studio aus einem geladenen Projekt wieder her.</summary>
    public async Task ApplyProjectDtoAsync(Audiola.Models.ProjectDto dto)
    {
        _suppressDirty = true;
        Tracks.Clear();
        SelectedClip = null;

        foreach (var td in dto.Tracks)
        {
            var firstMedia = td.Clips.FirstOrDefault()?.Media ?? "";
            var t = StemTrackViewModel.ForFile(firstMedia, td.Name, td.ColorHex);
            t.Volume = td.Volume;
            t.Pan = td.Pan;
            t.IsEnabled = td.IsEnabled;
            t.IsMuted = td.IsMuted;
            t.IsSolo = td.IsSolo;
            t.Lrc = td.Lrc;

            foreach (var cd in td.Clips)
            {
                if (string.IsNullOrEmpty(cd.Media)) continue;
                float[] peaks = [];
                var srcTotal = cd.SourceTotalSeconds;
                try
                {
                    var data = await _waveform.LoadAsync(cd.Media, 4000);
                    peaks = data.Peaks;
                    if (srcTotal <= 0) srcTotal = data.Duration.TotalSeconds;
                }
                catch { /* fehlende Medien überspringen wir gleich beim Engine-Load */ }

                var len = cd.LengthSeconds > 0 ? cd.LengthSeconds : srcTotal;
                var fStart = srcTotal > 0 ? cd.SourceStartSeconds / srcTotal : 0;
                var fEnd = srcTotal > 0 ? (cd.SourceStartSeconds + len) / srcTotal : 1;

                t.Clips.Add(new ClipViewModel
                {
                    Track = t,
                    SourcePath = cd.Media,
                    SourceTotalSeconds = srcTotal,
                    SourcePeaks = peaks,
                    TimelineOffsetSeconds = cd.TimelineOffsetSeconds,
                    SourceStartSeconds = cd.SourceStartSeconds,
                    LengthSeconds = len,
                    Peaks = SlicePeaks(peaks, fStart, fEnd),
                    GainDb = cd.GainDb,
                    FadeInSeconds = cd.FadeInSeconds,
                    FadeOutSeconds = cd.FadeOutSeconds
                });
            }

            if (t.Clips.Count > 0)
                t.LengthSeconds = t.Clips.Max(c => c.EndSeconds);

            Tracks.Add(t);
        }

        if (dto.PixelsPerSecond > 0) PixelsPerSecond = dto.PixelsPerSecond;
        SnapEnabled = dto.SnapEnabled;
        if (dto.GridSeconds > 0) GridSeconds = dto.GridSeconds;
        MasterVolume = dto.MasterVolume;

        OnPropertyChanged(nameof(HasTracks));
        RecomputeDuration();
        UpdateContentWidth();
        CommitClips();
        if (HasTracks) _transport.SetMode(TransportMode.StemMix);

        // Zuletzt ausgewählte Spur wiederherstellen.
        if (dto.SelectedTrackIndex >= 0 && dto.SelectedTrackIndex < Tracks.Count)
        {
            var clip = Tracks[dto.SelectedTrackIndex].Clips.FirstOrDefault();
            if (clip is not null) SelectClip(clip);
        }

        _suppressDirty = false;
        IsDirty = false;
        ResetHistory("Projekt geladen");
    }

    // ---- Verlauf (Undo / Redo / Sprung zu Zustand) ----

    private sealed record ClipSnap(string SourcePath, double SourceTotalSeconds, float[] SourcePeaks,
        double TimelineOffsetSeconds, double SourceStartSeconds, double LengthSeconds, float[] Peaks,
        double GainDb, double FadeInSeconds, double FadeOutSeconds);

    private sealed record TrackSnap(string Name, string AccentColor, string FilePath, double Volume, double Pan,
        bool IsEnabled, bool IsMuted, bool IsSolo, double StartOffsetSeconds, double LengthSeconds,
        float[] Peaks, List<ClipSnap> Clips);

    private sealed record StudioSnapshot(List<TrackSnap> Tracks, double MasterVolume);
    private sealed record HistoryState(StudioSnapshot Snap, string Label);

    private readonly List<HistoryState> _states = [];
    private int _index;             // aktueller Zustand in _states
    private const int MaxStates = 80;

    /// <summary>Anzeigeliste des Verlaufs (für das Verlauf-Panel).</summary>
    public ObservableCollection<HistoryEntryViewModel> History { get; } = [];

    public bool CanUndo => _index > 0;
    public bool CanRedo => _index < _states.Count - 1;

    /// <summary>Legt eine neue, leere Spur an (z. B. um Parts dorthin zu verschieben).</summary>
    [RelayCommand]
    private void AddEmptyTrack()
    {
        var track = StemTrackViewModel.ForFile("", $"Spur {Tracks.Count + 1}", Palette[Tracks.Count % Palette.Length]);
        Tracks.Add(track);
        OnPropertyChanged(nameof(HasTracks));
        SelectedClip = null;
        Commit("Leere Spur");
    }

    /// <summary>Nach dem Ziehen: Offsets in die Wiedergabe übernehmen.</summary>
    public void CommitClips()
    {
        _engine.Load(Tracks); // Load([]) entlädt sauber
        DurationSeconds = _engine.Duration.TotalSeconds;
        UpdatePlayhead();
    }

    partial void OnPixelsPerSecondChanged(double value)
    {
        UpdateContentWidth();
        UpdatePlayhead();
    }

    partial void OnDurationSecondsChanged(double value) => UpdateContentWidth();

    private void UpdateContentWidth() => OnPropertyChanged(nameof(ContentWidth));

    private void UpdatePlayhead()
        => PlayheadMargin = new Thickness(_engine.Position.TotalSeconds * PixelsPerSecond, 0, 0, 0);

    /// <summary>Klick auf die Zeitachse → an die entsprechende Zeit springen.</summary>
    public void SeekToPixel(double pixelX)
    {
        if (PixelsPerSecond <= 0) return;
        var seconds = Math.Clamp(pixelX / PixelsPerSecond, 0, DurationSeconds);
        _engine.Position = TimeSpan.FromSeconds(seconds);
        UpdatePlayhead();
    }

    [RelayCommand]
    private void ZoomIn() => PixelsPerSecond = Math.Min(MaxPps, PixelsPerSecond * 1.5);

    [RelayCommand]
    private void ZoomOut() => PixelsPerSecond = Math.Max(MinPps, PixelsPerSecond / 1.5);

    /// <summary>Zoomt so, dass der ganze Song in die sichtbare Breite passt (Fit). Vom View mit der Viewport-Breite aufgerufen.</summary>
    public void ZoomToFit(double viewportWidth)
    {
        if (DurationSeconds <= 0.01 || viewportWidth <= 40) return;
        PixelsPerSecond = Math.Clamp((viewportWidth - 28) / DurationSeconds, MinPps, MaxPps);
        UpdateContentWidth();
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (_engine.IsPlaying) _engine.Pause();
        else _engine.Play();
    }

    [RelayCommand]
    private void Stop() => _engine.Stop();
}
