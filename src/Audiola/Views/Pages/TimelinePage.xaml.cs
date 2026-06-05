using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Pages;

public partial class TimelinePage : Page, INavigableView<TimelineViewModel>, INavigationAware
{
    public TimelineViewModel ViewModel { get; }

    public TimelinePage(TimelineViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public void OnNavigatedTo() => ViewModel.OnActivated();

    public void OnNavigatedFrom() => ViewModel.OnDeactivated();

    private static readonly string[] AudioExt = [".wav", ".mp3", ".flac", ".aiff", ".aif", ".m4a", ".ogg"];

    private static string[] AudioFiles(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return [];
        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        return files.Where(f => AudioExt.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant())).ToArray();
    }

    private void Timeline_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = AudioFiles(e).Length > 0 ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Timeline_Drop(object sender, DragEventArgs e)
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
                var pps = ViewModel.PixelsPerSecond;
                if (pps > 0) offset = p.X / pps;
                var idx = (int)(p.Y / 108);
                if (idx >= 0 && idx < ViewModel.Tracks.Count) trackIndex = idx;
            }
            await ViewModel.AddAudioFileAsync(file, trackIndex, offset);
        }
    }

    private void Lane_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Ruler folgt horizontal, Spurköpfe folgen vertikal.
        RulerScroll.ScrollToHorizontalOffset(LaneScroll.HorizontalOffset);
        HeaderScroll.ScrollToVerticalOffset(LaneScroll.VerticalOffset);
    }

    // ---- Bereich direkt auf den Spuren aufziehen (Drag = Auswahl, Klick = Seek) ----
    private const double LaneH = 108;
    private bool _laneSelecting;
    private double _laneDownSeconds;
    private bool _laneMoved;
    private ViewModels.StemTrackViewModel? _laneTrack;

    private ViewModels.StemTrackViewModel? TrackAtY(double y)
    {
        var i = (int)(y / LaneH);
        return i >= 0 && i < ViewModel.Tracks.Count ? ViewModel.Tracks[i] : null;
    }

    private void Lanes_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.FrameworkElement fe) return;
        var pps = ViewModel.PixelsPerSecond;
        if (pps <= 0) return;
        var pos = e.GetPosition(fe);
        _laneSelecting = true;
        _laneMoved = false;
        _laneDownSeconds = pos.X / pps;
        _laneTrack = TrackAtY(pos.Y);
        ViewModel.SetSelection(_laneDownSeconds, _laneDownSeconds, _laneTrack);
        fe.CaptureMouse();
    }

    private void Lanes_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_laneSelecting || sender is not System.Windows.FrameworkElement fe) return;
        var pps = ViewModel.PixelsPerSecond;
        if (pps <= 0) return;
        var sec = e.GetPosition(fe).X / pps;
        if (Math.Abs(sec - _laneDownSeconds) * pps > 3) _laneMoved = true;
        if (_laneMoved) ViewModel.SetSelection(_laneDownSeconds, sec, _laneTrack);
    }

    private void Lanes_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement fe) fe.ReleaseMouseCapture();
        if (_laneSelecting && !_laneMoved)
            ViewModel.SeekToPixel(_laneDownSeconds * ViewModel.PixelsPerSecond); // reiner Klick → springen
        _laneSelecting = false;
    }

    // ---- Auswahlbereich auf dem Zeit-Lineal aufziehen ----
    private bool _selecting;
    private double _selStartSeconds;

    private void Ruler_Down(object sender, MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.FrameworkElement fe) return;
        var pps = ViewModel.PixelsPerSecond;
        if (pps <= 0) return;
        _selecting = true;
        _selStartSeconds = e.GetPosition(fe).X / pps;
        ViewModel.SetSelection(_selStartSeconds, _selStartSeconds);
        fe.CaptureMouse();
        e.Handled = true;
    }

    private void Ruler_Move(object sender, MouseEventArgs e)
    {
        if (!_selecting || sender is not System.Windows.FrameworkElement fe) return;
        var pps = ViewModel.PixelsPerSecond;
        if (pps <= 0) return;
        ViewModel.SetSelection(_selStartSeconds, e.GetPosition(fe).X / pps);
    }

    private void Ruler_Up(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement fe) fe.ReleaseMouseCapture();
        _selecting = false;
    }

    // ---- Clip auswählen, ziehen, Kanten trimmen ----
    private enum DragMode { Move, Left, Right }

    private const double EdgeGrip = 8;
    private ClipViewModel? _dragClip;
    private DragMode _dragMode;
    private double _dragStartX;
    private double _dragStartOffset;
    private bool _moved;
    private bool _undoPushed;

    // ---- Variationen-Provider anwenden ----
    private async void Variations_All_Click(object sender, RoutedEventArgs e)
        => await OpenVariationsAsync(ViewModel.Tracks.SelectMany(t => t.Clips).ToList(), "Gesamtes Audio (alle Spuren)");

    private async void ClipVariations_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedClip is { } c)
            await OpenVariationsAsync([c], "Ausgewählter Clip");
    }

    private async System.Threading.Tasks.Task OpenVariationsAsync(IReadOnlyList<ClipViewModel> clips, string scope)
    {
        var providers = ViewModel.VariationProviders;
        if (providers.Count == 0 || clips.Count == 0) return;

        var dlg = new Audiola.Views.Dialogs.VariationPickerWindow(providers, scope)
        {
            Owner = System.Windows.Window.GetWindow(this)
        };
        if (dlg.ShowDialog() != true || dlg.SelectedProvider is null || dlg.SelectedVariationIds.Count == 0) return;

        await ViewModel.ApplyVariationsAsync(dlg.SelectedProvider, dlg.SelectedVariationIds, clips);
    }

    // Rechtsklick wählt den Clip aus, damit das Kontextmenü darauf wirkt.
    private void Clip_RightDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement { DataContext: ClipViewModel clip })
            ViewModel.SelectClip(clip);
    }

    private bool _clipRangeSelecting;
    private double _rangeStartSeconds;
    private ViewModels.StemTrackViewModel? _rangeTrack;

    private void Clip_Down(object sender, MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.FrameworkElement fe || fe.DataContext is not ClipViewModel clip) return;

        // Doppelklick → Clip im Editor öffnen.
        if (e.ClickCount == 2)
        {
            ViewModel.SelectClip(clip);
            var editor = Audiola.App.GetService<EditorViewModel>();
            editor.LoadClipForEdit(clip);
            Audiola.App.GetService<Wpf.Ui.INavigationService>().Navigate(typeof(EditorPage));
            e.Handled = true;
            return;
        }

        // Auswahl-Werkzeug: Bereich aufziehen statt verschieben.
        if (ViewModel.RangeSelectMode)
        {
            ViewModel.SelectClip(clip);
            var pps0 = ViewModel.PixelsPerSecond;
            _rangeStartSeconds = pps0 > 0 ? e.GetPosition(LanesArea).X / pps0 : 0;
            _rangeTrack = clip.Track;
            _clipRangeSelecting = true;
            ViewModel.SetSelection(_rangeStartSeconds, _rangeStartSeconds, _rangeTrack);
            fe.CaptureMouse();
            e.Handled = true;
            return;
        }

        _dragClip = clip;
        _dragStartX = e.GetPosition(LanesArea).X;
        _dragStartOffset = clip.TimelineOffsetSeconds;
        _moved = false;
        _undoPushed = false;

        // Modus anhand der Position innerhalb des Clips bestimmen.
        var local = e.GetPosition(fe).X;
        var w = fe.ActualWidth;
        _dragMode = w < 24 ? DragMode.Move
            : local < EdgeGrip ? DragMode.Left
            : local > w - EdgeGrip ? DragMode.Right
            : DragMode.Move;

        ViewModel.SelectClip(clip);
        fe.CaptureMouse();
        e.Handled = true; // verhindert Seek auf der Lane
    }

    private void Clip_Move(object sender, MouseEventArgs e)
    {
        if (_clipRangeSelecting)
        {
            var p = ViewModel.PixelsPerSecond;
            if (p > 0) ViewModel.SetSelection(_rangeStartSeconds, e.GetPosition(LanesArea).X / p, _rangeTrack);
            return;
        }

        if (_dragClip is null) return;
        var pps = ViewModel.PixelsPerSecond;
        if (pps <= 0) return;
        var x = e.GetPosition(LanesArea).X;
        if (Math.Abs(x - _dragStartX) > 2 && !_moved)
        {
            _moved = true;
            if (!_undoPushed) { ViewModel.PushUndo(); _undoPushed = true; } // ein Undo-Schritt pro Geste
        }

        switch (_dragMode)
        {
            case DragMode.Left:
                ViewModel.SetClipLeftEdge(_dragClip, x / pps);
                break;
            case DragMode.Right:
                ViewModel.SetClipRightEdge(_dragClip, x / pps);
                break;
            default:
                ViewModel.SetClipOffset(_dragClip, _dragStartOffset + (x - _dragStartX) / pps);
                break;
        }
    }

    private const double LaneHeight = 108;

    private void Clip_Up(object sender, MouseButtonEventArgs e)
    {
        if (_clipRangeSelecting)
        {
            if (sender is System.Windows.FrameworkElement f) f.ReleaseMouseCapture();
            _clipRangeSelecting = false;
            e.Handled = true;
            return;
        }

        if (_dragClip is null) return;
        if (sender is System.Windows.FrameworkElement fe) fe.ReleaseMouseCapture();

        var clip = _dragClip;
        _dragClip = null;

        // Verschieben auf eine andere Spur (nur im Move-Modus, wenn wirklich gezogen wurde).
        if (_dragMode == DragMode.Move && _moved)
        {
            var pos = e.GetPosition(LanesArea);
            var targetIdx = (int)(pos.Y / LaneHeight);
            var currentIdx = ViewModel.Tracks.IndexOf(clip.Track);
            var pps = ViewModel.PixelsPerSecond;
            var newOffset = pps > 0 ? _dragStartOffset + (pos.X - _dragStartX) / pps : clip.TimelineOffsetSeconds;

            if (targetIdx >= 0 && targetIdx < ViewModel.Tracks.Count && targetIdx != currentIdx)
            {
                ViewModel.MoveClipToTrack(clip, targetIdx, newOffset);
                e.Handled = true;
                return;
            }
        }

        if (_moved) ViewModel.CommitClips();
        e.Handled = true;
    }
}
