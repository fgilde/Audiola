using System.Windows;
using System.Windows.Media;

namespace Audiola.Controls;

/// <summary>
/// Stereo-Master-Pegelanzeige (VU) mit zwei horizontalen Balken (L oben, R unten), dB-Skala
/// (−60..0 dB), sanftem Abfall und Peak-Hold-Strich. Gespeist per <see cref="SetLevels"/> im
/// Wiedergabe-Takt.
/// </summary>
public sealed class LevelMeterControl : FrameworkElement
{
    private float _l, _r;            // geglättete Anzeige (0..1 der dB-Skala)
    private float _peakL, _peakR;    // Peak-Hold
    private int _holdL, _holdR;      // verbleibende Halte-Ticks

    private static readonly Brush Track = Frozen(Color.FromArgb(0x55, 0x00, 0x00, 0x00));
    private static readonly Pen PeakPen = FrozenPen(Color.FromArgb(0xDD, 0xFF, 0xFF, 0xFF), 1.5);
    private static readonly Brush Fill = MakeGradient();

    public LevelMeterControl() => IsHitTestVisible = false;

    /// <summary>Neue Spitzenpegel (linear 0..1) übernehmen; rechnet in dB und glättet den Abfall.</summary>
    public void SetLevels(float l, float r)
    {
        _l = Ease(_l, ToDb01(l));
        _r = Ease(_r, ToDb01(r));
        Track1(ref _peakL, ref _holdL, _l);
        Track1(ref _peakR, ref _holdR, _r);
        InvalidateVisual();
    }

    private static float Ease(float cur, float target) => target > cur ? target : cur * 0.80f; // schnell rauf, weich runter

    private static void Track1(ref float peak, ref int hold, float v)
    {
        if (v >= peak) { peak = v; hold = 18; }        // neuer Peak → halten
        else if (--hold <= 0) peak = MathF.Max(0, peak - 0.03f);
    }

    private static float ToDb01(float lin)
    {
        if (lin <= 1e-5f) return 0f;
        var db = 20f * MathF.Log10(lin);
        return Math.Clamp((db + 60f) / 60f, 0f, 1f);    // −60..0 dB → 0..1
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (w < 4 || h < 4) return;
        double gap = 2, bh = (h - gap) / 2;

        DrawBar(dc, 0, w, bh, _l, _peakL);
        DrawBar(dc, bh + gap, w, bh, _r, _peakR);
    }

    private static void DrawBar(DrawingContext dc, double y, double w, double h, float level, float peak)
    {
        dc.DrawRoundedRectangle(Track, null, new Rect(0, y, w, h), 2, 2);
        if (level > 0.001)
            dc.DrawRoundedRectangle(Fill, null, new Rect(0, y, w * level, h), 2, 2);
        if (peak > 0.01)
        {
            double x = Math.Min(w - 1, w * peak);
            dc.DrawLine(PeakPen, new Point(x, y + 1), new Point(x, y + h - 1));
        }
    }

    private static Brush MakeGradient()
    {
        var g = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0x3D, 0xDC, 0x84), 0.0));  // grün
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0x9E, 0xE0, 0x3A), 0.55));
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xC2, 0x4B), 0.8));  // gelb
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x53, 0x50), 1.0));  // rot
        g.Freeze();
        return g;
    }

    private static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
    private static Pen FrozenPen(Color c, double t) { var p = new Pen(new SolidColorBrush(c), t); p.Freeze(); return p; }
}
