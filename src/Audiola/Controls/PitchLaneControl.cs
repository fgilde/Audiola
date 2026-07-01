using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Audiola.Dsp;
using Audiola.ViewModels;

namespace Audiola.Controls;

/// <summary>
/// SingStar-artiges Notenband: zeichnet die Referenz-Melodie (Soll) und den live gesungenen Verlauf
/// (Ist) in einem gleitenden Zeitfenster um die aktuelle Position. Treffer werden grün, Fehler rot.
/// Erwartet ein <see cref="SingAlongViewModel"/> als DataContext.
/// </summary>
public sealed class PitchLaneControl : FrameworkElement
{
    private const double WindowSec = 6.0;   // sichtbares Zeitfenster (Position in der Mitte)
    private const double MidiMin = 45;      // A2
    private const double MidiMax = 76;      // E5

    private static readonly Brush BgBrush = Frozen(Color.FromRgb(0x12, 0x12, 0x18));
    private static readonly Brush GridBrush = Frozen(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF));
    private static readonly Brush RefBrush = Frozen(Color.FromArgb(0xCC, 0x6B, 0xD6, 0xFF));
    private static readonly Brush HitBrush = Frozen(Color.FromRgb(0x4C, 0xD9, 0x64));
    private static readonly Brush MissBrush = Frozen(Color.FromRgb(0xFF, 0x6B, 0x6B));
    private static readonly Pen PlayheadPen = FrozenPen(Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF), 1.5);

    private SingAlongViewModel? _vm;

    public PitchLaneControl()
    {
        IsHitTestVisible = false;
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) => Detach();
    }

    private void OnDataContextChanged(object? s, DependencyPropertyChangedEventArgs e)
    {
        Detach();
        _vm = e.NewValue as SingAlongViewModel;
        if (_vm is not null)
        {
            _vm.PitchUpdated += OnData;
            _vm.PropertyChanged += OnVmProperty;
        }
        InvalidateVisual();
    }

    private void Detach()
    {
        if (_vm is null) return;
        _vm.PitchUpdated -= OnData;
        _vm.PropertyChanged -= OnVmProperty;
        _vm = null;
    }

    private void OnData(object? s, EventArgs e) => InvalidateVisual();

    private void OnVmProperty(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SingAlongViewModel.PositionSeconds)) InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        dc.DrawRectangle(BgBrush, null, new Rect(0, 0, w, h));
        if (_vm is null || w < 4 || h < 4) return;

        double pos = _vm.PositionSeconds;
        double t0 = pos - WindowSec / 2, t1 = pos + WindowSec / 2;

        double X(double t) => (t - t0) / WindowSec * w;
        double Y(double midi) => h - (midi - MidiMin) / (MidiMax - MidiMin) * h;

        // Oktav-Gitterlinien (C-Noten).
        for (int m = 48; m <= 72; m += 12)
        {
            double y = Y(m);
            dc.DrawLine(new Pen(GridBrush, 1), new Point(0, y), new Point(w, y));
        }

        // Referenz-Melodie (Soll) als Punkte im Fenster.
        var reference = _vm.Reference;
        foreach (var p in reference)
        {
            if (p.Hz <= 0 || p.TimeSeconds < t0 || p.TimeSeconds > t1) continue;
            double midi = PitchDetector.HzToMidi(p.Hz);
            if (midi < MidiMin || midi > MidiMax) continue;
            double x = X(p.TimeSeconds);
            dc.DrawRoundedRectangle(RefBrush, null, new Rect(x - 2, Y(midi) - 3, 5, 6), 2, 2);
        }

        // Gesungener Verlauf (Ist).
        var sung = _vm.SungHistory;
        for (int i = 0; i < sung.Count; i++)
        {
            var s = sung[i];
            if (s.Hz <= 0 || s.TimeSeconds < t0 || s.TimeSeconds > t1) continue;
            double midi = PitchDetector.HzToMidi(s.Hz);
            if (midi < MidiMin || midi > MidiMax) continue;
            double tref = ReferenceHzAt(reference, s.TimeSeconds);
            bool hit = tref > 0 && Math.Abs(PitchDetector.CentsOffOctaveless(s.Hz, tref)) < 100;
            dc.DrawEllipse(hit ? HitBrush : MissBrush, null, new Point(X(s.TimeSeconds), Y(midi)), 3.5, 3.5);
        }

        // Playhead in der Mitte.
        double cx = X(pos);
        dc.DrawLine(PlayheadPen, new Point(cx, 0), new Point(cx, h));
    }

    private static double ReferenceHzAt(IReadOnlyList<PitchPoint> reference, double t)
    {
        // lineare Nähe-Suche im kleinen Fenster reicht (wird selten aufgerufen)
        double best = 0, bestDt = 0.12;
        for (int i = 0; i < reference.Count; i++)
        {
            double dt = Math.Abs(reference[i].TimeSeconds - t);
            if (dt <= bestDt && reference[i].Hz > 0) { best = reference[i].Hz; bestDt = dt; }
        }
        return best;
    }

    private static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
    private static Pen FrozenPen(Color c, double t) { var p = new Pen(new SolidColorBrush(c), t); p.Freeze(); return p; }
}
