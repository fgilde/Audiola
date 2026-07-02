using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using Singola.Services;
using Singola.ViewModels;

namespace Singola;

/// <summary>
/// Die Singola-Bühne: hostet Setup/Singen/Ergebnis und zeichnet die Live-Pitch-Linien
/// aller Spieler (scrollende Zeitachse per TranslateTransform — Punkte liegen in
/// absoluten Zeit-Koordinaten, pro Frame wird nur die Transform verschoben).
/// </summary>
public partial class MainWindow
{
    private const double PxPerSecond = 90;     // Zeitachse der Bühne
    private const double MidiLow = 38, MidiHigh = 82;
    private const double NowAnchor = 0.88;     // „Jetzt" liegt bei 88 % der Breite

    public MainViewModel ViewModel { get; }
    public string? PendingStartupFile { get; set; }

    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(50) };
    private readonly TranslateTransform _scroll = new();
    private readonly Polyline?[] _activeLines = new Polyline?[4];

    public MainWindow()
    {
        ViewModel = new MainViewModel();
        DataContext = ViewModel;
        InitializeComponent();

        Stage.RenderTransform = _scroll;
        ViewModel.PitchSampled += OnPitchSampled;
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsSinging) && ViewModel.IsSinging)
                ResetStage();
        };

        // Mic-/Spieleränderungen → Start-Button-Validierung (Geräte müssen eindeutig sein).
        void HookSlot(PlayerSlot s) => s.PropertyChanged += (_, a) =>
        { if (a.PropertyName == nameof(PlayerSlot.DeviceNumber)) ViewModel.RevalidateStart(); };
        foreach (var s in ViewModel.Players) HookSlot(s);
        ViewModel.Players.CollectionChanged += (_, a) =>
        {
            foreach (PlayerSlot s in a.NewItems ?? Array.Empty<PlayerSlot>()) HookSlot(s);
            ViewModel.RevalidateStart();
        };

        _tick.Tick += (_, _) =>
        {
            ViewModel.Tick();
            if (ViewModel.IsSinging)
            {
                DrawMelodyNotes();   // beim ersten Tick, sobald das Canvas Maße hat
                _scroll.X = Stage.ActualWidth * NowAnchor - ViewModel.Engine.PositionSeconds * PxPerSecond;
                NowLine.X = Stage.ActualWidth * NowAnchor;
            }
        };
        _tick.Start();

        Loaded += async (_, _) =>
        {
            if (PendingStartupFile is { } f) { PendingStartupFile = null; await ViewModel.LoadSongAsync(f); }
        };
        Closed += (_, _) => { _tick.Stop(); ViewModel.Engine.Dispose(); };
    }

    // ---- Song öffnen ----

    private async void OpenSong_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Song öffnen",
            Filter = "Songs & Projekte|*.mp3;*.wav;*.ogg;*.flac;*.m4a;*.aac;*.wma;*.mp4;*.audiola|Alle Dateien|*.*",
        };
        if (dlg.ShowDialog() == true) await ViewModel.LoadSongAsync(dlg.FileName);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        var ok = e.Data.GetDataPresent(DataFormats.FileDrop)
                 && ((string[])e.Data.GetData(DataFormats.FileDrop)!).Any(SongLoader.IsSupported);
        e.Effects = ok ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var file = ((string[])e.Data.GetData(DataFormats.FileDrop)!).FirstOrDefault(SongLoader.IsSupported);
        if (file is not null) await ViewModel.LoadSongAsync(file);
    }

    // ---- Bühnen-Zeichnung ----

    private bool _notesDrawn;

    private void ResetStage()
    {
        Stage.Children.Clear();
        Array.Clear(_activeLines);
        _scroll.X = 0;
        _notesDrawn = false;
    }

    /// <summary>Zeichnet das komplette Notenband (Ziel-Noten) einmal in absoluten Zeit-Koordinaten.</summary>
    private void DrawMelodyNotes()
    {
        if (_notesDrawn || Stage.ActualHeight < 10) return;
        _notesDrawn = true;
        var h = Stage.ActualHeight;
        var noteHeight = Math.Max(7, h / (MidiHigh - MidiLow) * 1.7);
        foreach (var n in ViewModel.Melody)
        {
            var y = h * (1 - (Math.Clamp(n.Midi, MidiLow, MidiHigh) - MidiLow) / (MidiHigh - MidiLow));
            var bar = new System.Windows.Shapes.Rectangle
            {
                Width = Math.Max(6, (n.End - n.Start) * PxPerSecond),
                Height = noteHeight,
                RadiusX = noteHeight / 2, RadiusY = noteHeight / 2,
                Fill = new SolidColorBrush(Color.FromArgb(0x38, 0xFF, 0xFF, 0xFF)),
                Stroke = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                StrokeThickness = 1,
            };
            System.Windows.Controls.Canvas.SetLeft(bar, n.Start * PxPerSecond);
            System.Windows.Controls.Canvas.SetTop(bar, y - noteHeight / 2);
            Stage.Children.Add(bar);
        }
    }

    private void OnPitchSampled(int player, double t, double midi)
    {
        if (player >= _activeLines.Length || Stage.ActualHeight < 10) return;

        if (midi <= 0) { _activeLines[player] = null; return; }   // Stille → Linie beenden

        var x = t * PxPerSecond;
        var y = Stage.ActualHeight * (1 - (Math.Clamp(midi, MidiLow, MidiHigh) - MidiLow) / (MidiHigh - MidiLow));

        var line = _activeLines[player];
        if (line is null)
        {
            var color = (Color)ColorConverter.ConvertFromString(ViewModel.Players[player].ColorHex);
            line = new Polyline
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 4.5,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Effect = new DropShadowEffect { Color = color, BlurRadius = 14, ShadowDepth = 0, Opacity = 0.9 },
            };
            Stage.Children.Add(line);
            _activeLines[player] = line;
        }
        line.Points.Add(new Point(x, y));

        // Aufräumen: Linien, die komplett links aus dem Bild gescrollt sind.
        if (Stage.Children.Count > 60)
        {
            var cutoff = (ViewModel.Engine.PositionSeconds - 30) * PxPerSecond;
            for (var i = Stage.Children.Count - 1; i >= 0; i--)
                if (Stage.Children[i] is Polyline p && p != _activeLines.FirstOrDefault(l => l == p)
                    && p.Points.Count > 0 && p.Points[^1].X < cutoff)
                    Stage.Children.RemoveAt(i);
        }
    }
}
