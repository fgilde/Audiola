using Audiola.Avalonia.Platform;
using Audiola.Services;
using Audiola.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Audiola.Avalonia.Views.Pages;

public partial class TimelinePage : UserControl, INavigationAware
{
    private readonly TimelineViewModel _vm;

    public TimelinePage(TimelineViewModel viewModel)
    {
        _vm = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        _vm.PropertyChanged += Vm_PropertyChanged;

        // Ruler folgt horizontal, Spurköpfe folgen vertikal.
        LaneScroll.ScrollChanged += (_, _) =>
        {
            RulerScroll.Offset = RulerScroll.Offset.WithX(LaneScroll.Offset.X);
            HeaderScroll.Offset = HeaderScroll.Offset.WithY(LaneScroll.Offset.Y);
        };
        LaneScroll.AddHandler(PointerWheelChangedEvent, Lane_PointerWheel, RoutingStrategies.Tunnel);

        // Dateien in die Arbeitsfläche ziehen.
        AddHandler(DragDrop.DragOverEvent, Timeline_DragOver);
        AddHandler(DragDrop.DropEvent, Timeline_Drop);
    }

    public void OnNavigatedTo() => _vm.OnActivated();

    public void OnNavigatedFrom() => _vm.OnDeactivated();

    // ---- Auto-Scroll: Playhead bei Wiedergabe im sichtbaren Bereich halten ----
    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimelineViewModel.PlayheadX) && _vm.IsPlaying)
            EnsurePlayheadVisible();
    }

    private void EnsurePlayheadVisible()
    {
        var x = _vm.PlayheadX;
        var left = LaneScroll.Offset.X;
        var vw = LaneScroll.Viewport.Width;
        if (vw <= 0) return;
        if (x > left + vw - 60) ScrollLanesTo(x - 60);          // rechte Kante → umblättern
        else if (x < left + 20) ScrollLanesTo(x - 20);
    }

    private void ScrollLanesTo(double x) =>
        LaneScroll.Offset = LaneScroll.Offset.WithX(Math.Max(0, x));

    // ---- Playhead ziehen (Scrubbing) ----
    private bool _scrubbing;

    private void Playhead_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _scrubbing = true;
        if (sender is Control control) e.Pointer.Capture(control);
        _vm.SeekToPixel(e.GetPosition(Ruler).X);
        e.Handled = true;
    }

    private void Playhead_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_scrubbing) _vm.SeekToPixel(e.GetPosition(Ruler).X);
    }

    private void Playhead_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        e.Pointer.Capture(null);
        _scrubbing = false;
        e.Handled = true;
    }

    // ---- HQ-Trennung (audio-separator) ----
    private async void SeparateHq_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string key, DataContext: StemTrackViewModel track })
            await _vm.SeparateTrackHqAsync(track, key);
    }

    // ---- Spurfarbe setzen (Kontextmenü) ----
    private void TrackColor_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string hex, DataContext: StemTrackViewModel track })
            track.CustomColor = hex;
    }

    // ---- Spur auswählen (Kopf anklicken) ----
    private void TrackHeader_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: StemTrackViewModel track })
            _vm.SelectTrack(track);
    }

    // ---- Drag & Drop von Audiodateien ----
    private static readonly string[] AudioExt = [".wav", ".mp3", ".flac", ".aiff", ".aif", ".m4a", ".ogg"];

    private static string[] AudioFiles(DragEventArgs e) =>
    [
        .. (e.Data.GetFiles() ?? [])
            .Select(f => f.Path.LocalPath)
            .Where(f => AudioExt.Contains(Path.GetExtension(f).ToLowerInvariant()))
    ];

    private static void Timeline_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = AudioFiles(e).Length > 0 ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Timeline_Drop(object? sender, DragEventArgs e)
    {
        var files = AudioFiles(e);
        if (files.Length == 0) return;
        e.Handled = true; // verhindert den globalen Fenster-Drop

        foreach (var file in files)
        {
            var trackIndex = -1;
            var offset = 0.0;
            if (LanesArea.IsVisible)
            {
                var p = e.GetPosition(LanesArea);
                var pps = _vm.PixelsPerSecond;
                if (pps > 0) offset = p.X / pps;
                var idx = (int)(p.Y / Math.Max(1, _vm.LaneHeight));
                if (idx >= 0 && idx < _vm.Tracks.Count) trackIndex = idx;
            }
            await _vm.AddAudioFileAsync(file, trackIndex, offset);
        }
    }

    /// <summary>„Auf Song einpassen": Zoom so setzen, dass der ganze Song in die sichtbare Breite passt.</summary>
    private void Fit_Click(object? sender, RoutedEventArgs e)
    {
        _vm.ZoomToFit(LaneScroll.Viewport.Width);
        ScrollLanesTo(0);
    }

    /// <summary>Strg+Mausrad zoomt die Timeline um die Maus-Position (DAW-Standard).</summary>
    private void Lane_PointerWheel(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;
        e.Handled = true;

        // Zeitpunkt unter der Maus vor dem Zoom merken …
        var mouseX = e.GetPosition(LaneScroll).X;
        var oldPps = _vm.PixelsPerSecond;
        if (oldPps <= 0) return;
        var timeAtMouse = (LaneScroll.Offset.X + mouseX) / oldPps;

        if (e.Delta.Y > 0) _vm.ZoomInCommand.Execute(null);
        else _vm.ZoomOutCommand.Execute(null);

        // … und danach so scrollen, dass derselbe Zeitpunkt unter der Maus bleibt.
        var newPps = _vm.PixelsPerSecond;
        if (Math.Abs(newPps - oldPps) < 0.001) return;
        ScrollLanesTo(timeAtMouse * newPps - mouseX);
    }

    // ---- Bereich direkt auf den Spuren aufziehen (Drag = Auswahl, Klick = Seek) ----
    private bool _laneSelecting;
    private double _laneDownSeconds;
    private bool _laneMoved;
    private StemTrackViewModel? _laneTrack;

    private StemTrackViewModel? TrackAtY(double y)
    {
        var i = (int)(y / Math.Max(1, _vm.LaneHeight));
        return i >= 0 && i < _vm.Tracks.Count ? _vm.Tracks[i] : null;
    }

    private void Lanes_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        var pps = _vm.PixelsPerSecond;
        if (pps <= 0) return;
        var pos = e.GetPosition(control);
        _laneSelecting = true;
        _laneMoved = false;
        _laneDownSeconds = pos.X / pps;
        _laneTrack = TrackAtY(pos.Y);
        if (_laneTrack is not null) _vm.SelectTrack(_laneTrack);
        _vm.SetSelection(_laneDownSeconds, _laneDownSeconds, _laneTrack);
        e.Pointer.Capture(control);
    }

    private void Lanes_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_laneSelecting || sender is not Control control) return;
        var pps = _vm.PixelsPerSecond;
        if (pps <= 0) return;
        var sec = e.GetPosition(control).X / pps;
        if (Math.Abs(sec - _laneDownSeconds) * pps > 3) _laneMoved = true;
        if (_laneMoved) _vm.SetSelection(_laneDownSeconds, sec, _laneTrack);
    }

    private void Lanes_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        e.Pointer.Capture(null);
        if (_laneSelecting && !_laneMoved)
            _vm.SeekToPixel(_laneDownSeconds * _vm.PixelsPerSecond); // reiner Klick → springen
        _laneSelecting = false;
    }

    // ---- Auswahlbereich auf dem Zeit-Lineal aufziehen ----
    private bool _selecting;
    private double _selStartSeconds;
    private bool _rulerDragged;

    // DAW-Verhalten in der Zeitleiste: reiner KLICK springt mit Playhead + Zeit dorthin,
    // ZIEHEN zieht wie bisher eine Auswahl (Loop-/Export-Bereich) auf.
    private void Ruler_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        var pps = _vm.PixelsPerSecond;
        if (pps <= 0) return;
        _selecting = true;
        _rulerDragged = false;
        _selStartSeconds = e.GetPosition(control).X / pps;
        e.Pointer.Capture(control);
        e.Handled = true;
    }

    private void Ruler_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_selecting || sender is not Control control) return;
        var pps = _vm.PixelsPerSecond;
        if (pps <= 0) return;
        var cur = e.GetPosition(control).X / pps;
        if (!_rulerDragged && Math.Abs(cur - _selStartSeconds) * pps < 4) return; // Klick-Toleranz
        _rulerDragged = true;
        _vm.SetSelection(_selStartSeconds, cur);
    }

    private void Ruler_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        e.Pointer.Capture(null);
        if (_selecting && !_rulerDragged)
            _vm.SeekToPixel(_selStartSeconds * _vm.PixelsPerSecond); // reiner Klick → springen
        _selecting = false;
    }

    // ---- Variationen-Provider anwenden ----
    private async void Variations_All_Click(object? sender, RoutedEventArgs e)
        => await OpenVariationsAsync([.. _vm.Tracks.SelectMany(t => t.Clips)], "Gesamtes Audio (alle Spuren)");

    private async void ClipVariations_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm.SelectedClip is { } clip)
            await OpenVariationsAsync([clip], "Ausgewählter Clip");
    }

    private async Task OpenVariationsAsync(IReadOnlyList<ClipViewModel> clips, string scope)
    {
        var providers = _vm.VariationProviders;
        if (providers.Count == 0 || clips.Count == 0) return;

        var choice = await AppServices.Get<IAppDialogs>().PickVariationsAsync(providers, scope);
        if (choice is null) return;

        await _vm.ApplyVariationsAsync(choice.Provider, choice.VariationIds, clips);
    }

    // ---- Stimme tauschen (Dialog: wählen / aufnehmen / hochladen) ----
    private async void ClipVoiceChange_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm.SelectedClip is null) return;
        if (await AppServices.Get<IAppDialogs>().PickVoiceAsync() is { } choice)
            await _vm.ChangeSelectedClipVoiceAsync(choice);
    }

    // ---- Transkription (Whisper / ElevenLabs → LRC) ----
    private async void Transcribe_Click(object? sender, RoutedEventArgs e)
        => await _vm.TranscribeSelectedClipAsync();

    private async void TranscribeEleven_Click(object? sender, RoutedEventArgs e)
        => await _vm.TranscribeSelectedClipAsync(useElevenLabs: true);

    // ---- Spur aus Text (TTS) ----
    private async void AddTts_Click(object? sender, RoutedEventArgs e)
    {
        if (await AppServices.Get<IAppDialogs>().AskTextToSpeechAsync() is { } request)
            await _vm.AddTextToSpeechTrackAsync(request.Text, request.Voice, request.Speed,
                request.Stability, request.Similarity);
    }

    // ---- Fade-Griffe: an der Kante nach unten ziehen = Ein-/Ausblenden ----
    private ClipViewModel? _fadeClip;
    private double _fadeStartY, _fadeStartVal;
    private bool _fadeIsIn;

    private void FadeIn_PointerPressed(object? sender, PointerPressedEventArgs e) => StartFade(sender, e, true);

    private void FadeOut_PointerPressed(object? sender, PointerPressedEventArgs e) => StartFade(sender, e, false);

    private void StartFade(object? sender, PointerPressedEventArgs e, bool isIn)
    {
        if (sender is not Control { DataContext: ClipViewModel clip } control) return;
        _fadeClip = clip;
        _fadeIsIn = isIn;
        _fadeStartY = e.GetPosition(LanesArea).Y;
        _fadeStartVal = isIn ? clip.FadeInSeconds : clip.FadeOutSeconds;
        e.Pointer.Capture(control);
        e.Handled = true; // nicht den Clip verschieben
    }

    private void Fade_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_fadeClip is null) return;
        var dy = e.GetPosition(LanesArea).Y - _fadeStartY;
        var f = _fadeStartVal + dy / Math.Max(1, _vm.LaneHeight) * _fadeClip.LengthSeconds;
        f = Math.Clamp(f, 0, _fadeClip.LengthSeconds);
        if (_fadeIsIn) _fadeClip.FadeInSeconds = f;
        else _fadeClip.FadeOutSeconds = f;
    }

    private void Fade_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_fadeClip is null) return;
        e.Pointer.Capture(null);
        _fadeClip = null;
        _vm.Commit(_fadeIsIn ? "Fade-In" : "Fade-Out");
        e.Handled = true;
    }

    // ---- Clip auswählen, ziehen, Kanten trimmen ----
    private enum DragMode { Move, Left, Right }

    private const double EdgeGrip = 8;
    private ClipViewModel? _dragClip;
    private DragMode _dragMode;
    private double _dragStartX;
    private double _dragStartOffset;
    private bool _moved;

    // Kanten-Ziehen: Standard = Zeitdehnung (schneller/langsamer), mit Strg = Trimmen (schneiden).
    private bool _stretchDrag;
    private double _stretchOrigLen, _stretchOrigSrcStart, _stretchOrigOffset;

    private bool _clipRangeSelecting;
    private double _rangeStartSeconds;
    private StemTrackViewModel? _rangeTrack;

    private void Clip_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: ClipViewModel clip } control) return;

        // Rechtsklick wählt den Clip aus, damit das Kontextmenü darauf wirkt.
        if (e.GetCurrentPoint(control).Properties.IsRightButtonPressed)
        {
            _vm.SelectClip(clip);
            return;
        }

        // Doppelklick → Clip im Editor öffnen.
        if (e.ClickCount == 2)
        {
            _vm.SelectClip(clip);
            AppServices.Get<EditorViewModel>().LoadClipForEdit(clip);
            AppServices.Get<IShellNavigation>().Navigate(ShellPage.Editor);
            e.Handled = true;
            return;
        }

        // Auswahl-Werkzeug: Bereich aufziehen statt verschieben.
        if (_vm.RangeSelectMode)
        {
            _vm.SelectClip(clip);
            var pps0 = _vm.PixelsPerSecond;
            _rangeStartSeconds = pps0 > 0 ? e.GetPosition(LanesArea).X / pps0 : 0;
            _rangeTrack = clip.Track;
            _clipRangeSelecting = true;
            _vm.SetSelection(_rangeStartSeconds, _rangeStartSeconds, _rangeTrack);
            e.Pointer.Capture(control);
            e.Handled = true;
            return;
        }

        _dragClip = clip;
        _dragStartX = e.GetPosition(LanesArea).X;
        _dragStartOffset = clip.TimelineOffsetSeconds;
        _moved = false;

        // Modus anhand der Position innerhalb des Clips bestimmen.
        var local = e.GetPosition(control).X;
        var w = control.Bounds.Width;
        _dragMode = w < 24 ? DragMode.Move
            : local < EdgeGrip ? DragMode.Left
            : local > w - EdgeGrip ? DragMode.Right
            : DragMode.Move;

        // An einer Kante: Standard = Dehnen; mit gedrückter Strg-Taste = Trimmen (Schneiden).
        _stretchDrag = _dragMode != DragMode.Move && !e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (_stretchDrag)
        {
            _stretchOrigLen = clip.LengthSeconds;
            _stretchOrigSrcStart = clip.SourceStartSeconds;
            _stretchOrigOffset = clip.TimelineOffsetSeconds;
        }

        _vm.SelectClip(clip);
        e.Pointer.Capture(control);
        e.Handled = true; // verhindert Seek auf der Lane
    }

    private void Clip_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_clipRangeSelecting)
        {
            var p = _vm.PixelsPerSecond;
            if (p > 0) _vm.SetSelection(_rangeStartSeconds, e.GetPosition(LanesArea).X / p, _rangeTrack);
            return;
        }

        if (_dragClip is null) return;
        var pps = _vm.PixelsPerSecond;
        if (pps <= 0) return;
        var x = e.GetPosition(LanesArea).X;
        if (Math.Abs(x - _dragStartX) > 2) _moved = true;

        switch (_dragMode)
        {
            case DragMode.Left:
                if (_stretchDrag) _vm.SetClipStretchEdge(_dragClip, x / pps, fromLeft: true, _stretchOrigOffset, _stretchOrigLen);
                else _vm.SetClipLeftEdge(_dragClip, x / pps);
                break;
            case DragMode.Right:
                if (_stretchDrag) _vm.SetClipStretchEdge(_dragClip, x / pps, fromLeft: false, _stretchOrigOffset, _stretchOrigLen);
                else _vm.SetClipRightEdge(_dragClip, x / pps);
                break;
            default:
                _vm.SetClipOffset(_dragClip, _dragStartOffset + (x - _dragStartX) / pps);
                break;
        }
    }

    private async void Clip_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_clipRangeSelecting)
        {
            e.Pointer.Capture(null);
            _clipRangeSelecting = false;
            e.Handled = true;
            return;
        }

        if (_dragClip is null) return;
        e.Pointer.Capture(null);

        var clip = _dragClip;
        _dragClip = null;

        // Kante gedehnt: Audio einmalig per Time-Stretch auf die neue Länge bringen (Tonhöhe bleibt).
        if (_stretchDrag && _moved)
        {
            await _vm.StretchClipToLengthAsync(clip, clip.LengthSeconds, _stretchOrigSrcStart, _stretchOrigLen);
            e.Handled = true;
            return;
        }

        // Verschieben auf eine andere Spur (nur im Move-Modus, wenn wirklich gezogen wurde).
        if (_dragMode == DragMode.Move && _moved)
        {
            var pos = e.GetPosition(LanesArea);
            var targetIdx = (int)(pos.Y / Math.Max(1, _vm.LaneHeight));
            var currentIdx = _vm.Tracks.IndexOf(clip.Track);
            var pps = _vm.PixelsPerSecond;
            var newOffset = pps > 0 ? _dragStartOffset + (pos.X - _dragStartX) / pps : clip.TimelineOffsetSeconds;

            if (targetIdx >= 0 && targetIdx < _vm.Tracks.Count && targetIdx != currentIdx)
            {
                _vm.MoveClipToTrack(clip, targetIdx, newOffset);
                _vm.Commit("Clip auf andere Spur");
                e.Handled = true;
                return;
            }
        }

        if (_moved)
        {
            _vm.CommitClips();
            _vm.Commit(_dragMode == DragMode.Move ? "Clip verschoben" : "Clip getrimmt");
        }
        e.Handled = true;
    }
}
