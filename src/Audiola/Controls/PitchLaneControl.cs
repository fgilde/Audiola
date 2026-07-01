using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Audiola.Dsp;
using Audiola.ViewModels;

namespace Audiola.Controls;

/// <summary>
/// SingStar-artiges Notenband: die Referenz-Melodie erscheint als Notenblöcke (Soll), der live
/// gesungene Ton als durchgehende Linie (Ist) — grün bei Treffer, rot daneben — in einem gleitenden
/// Zeitfenster um die aktuelle Position. Erwartet ein <see cref="SingAlongViewModel"/> als DataContext.
/// </summary>
public sealed class PitchLaneControl : FrameworkElement
{
    private const double WindowSec = 6.0;   // sichtbares Zeitfenster (Playhead in der Mitte)
    private const double MidiMin = 45;      // A2
    private const double MidiMax = 76;      // E5

    private static readonly Brush BgBrush = Frozen(Color.FromRgb(0x12, 0x12, 0x18));
    private static readonly Brush GridBrush = Frozen(Color.FromArgb(0x1C, 0xFF, 0xFF, 0xFF));
    private static readonly Brush RefBar = Frozen(Color.FromArgb(0xFF, 0x4A, 0x7C, 0xC7));   // Soll-Note
    private static readonly Pen HitPen = FrozenPen(Color.FromRgb(0x4C, 0xD9, 0x64), 3.0);    // Treffer
    private static readonly Pen MissPen = FrozenPen(Color.FromRgb(0xFF, 0x8A, 0x5B), 3.0);   // daneben
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

        // Oktav-Gitter (C-Linien).
        for (int m = 48; m <= 72; m += 12)
            dc.DrawLine(new Pen(GridBrush, 1), new Point(0, Y(m)), new Point(w, Y(m)));

        // Referenz-Melodie als Notenblöcke: aufeinanderfolgende Punkte gleicher Note zusammenfassen.
        var reference = _vm.Reference;
        int i = 0;
        while (i < reference.Count)
        {
            var p = reference[i];
            if (p.Hz <= 0 || p.TimeSeconds > t1) { i++; continue; }
            int note = (int)Math.Round(PitchDetector.HzToMidi(p.Hz));
            int j = i;
            while (j + 1 < reference.Count && reference[j + 1].Hz > 0 &&
                   (int)Math.Round(PitchDetector.HzToMidi(reference[j + 1].Hz)) == note &&
                   reference[j + 1].TimeSeconds - reference[j].TimeSeconds < 0.2)
                j++;

            double segStart = reference[i].TimeSeconds, segEnd = reference[j].TimeSeconds;
            if (segEnd >= t0 && segStart <= t1 && note >= MidiMin && note <= MidiMax)
            {
                double x1 = Math.Max(0, X(segStart)), x2 = Math.Min(w, X(segEnd));
                dc.DrawRoundedRectangle(RefBar, null, new Rect(x1, Y(note) - 4, Math.Max(6, x2 - x1), 8), 4, 4);
            }
            i = j + 1;
        }

        // Gesungener Verlauf als durchgehende Linie (bei stimmlosen Lücken unterbrochen).
        var sung = _vm.SungHistory;
        Point? prev = null;
        for (int k = 0; k < sung.Count; k++)
        {
            var s = sung[k];
            if (s.Hz <= 0 || s.TimeSeconds < t0 || s.TimeSeconds > t1) { prev = null; continue; }
            double midi = PitchDetector.HzToMidi(s.Hz);
            if (midi < MidiMin || midi > MidiMax) { prev = null; continue; }
            var pt = new Point(X(s.TimeSeconds), Y(midi));
            double tref = ReferenceHzAt(reference, s.TimeSeconds);
            bool hit = tref > 0 && Math.Abs(PitchDetector.CentsOffOctaveless(s.Hz, tref)) < 100;
            if (prev is { } pv) dc.DrawLine(hit ? HitPen : MissPen, pv, pt);
            else dc.DrawEllipse(hit ? HitPen.Brush : MissPen.Brush, null, pt, 2.5, 2.5);
            prev = pt;
        }

        // Playhead in der Mitte.
        double cx = X(pos);
        dc.DrawLine(PlayheadPen, new Point(cx, 0), new Point(cx, h));
    }

    private static double ReferenceHzAt(IReadOnlyList<PitchPoint> reference, double t)
    {
        double best = 0, bestDt = 0.12;
        for (int i = 0; i < reference.Count; i++)
        {
            if (reference[i].Hz <= 0) continue;
            double dt = Math.Abs(reference[i].TimeSeconds - t);
            if (dt <= bestDt) { best = reference[i].Hz; bestDt = dt; }
        }
        return best;
    }

    private static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
    private static Pen FrozenPen(Color c, double t) { var p = new Pen(new SolidColorBrush(c), t); p.Freeze(); return p; }
}
