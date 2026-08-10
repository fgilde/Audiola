using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Audiola.Controls;

/// <summary>
/// Stereo-Master-Pegelanzeige (VU) mit zwei horizontalen Balken (L oben, R unten), dB-Skala
/// (−60..0 dB), sanftem Abfall und Peak-Hold-Strich. Gespeist per <see cref="SetLevels"/> im
/// Wiedergabe-Takt.
/// </summary>
public sealed class LevelMeterControl : Control
{
    private static readonly IBrush Track = new SolidColorBrush(Color.FromArgb(0x55, 0x00, 0x00, 0x00));
    private static readonly IPen PeakPen = new Pen(new SolidColorBrush(Color.FromArgb(0xDD, 0xFF, 0xFF, 0xFF)), 1.5);
    private static readonly IBrush ClipLed = new SolidColorBrush(Color.FromRgb(0xFF, 0x35, 0x30));
    private static readonly IBrush Fill = MakeGradient();

    private float _l, _r;            // geglättete Anzeige (0..1 der dB-Skala)
    private float _peakL, _peakR;    // Peak-Hold
    private int _holdL, _holdR;      // verbleibende Halte-Ticks
    private bool _clip;              // Übersteuerung — latcht bis zum Klick

    public LevelMeterControl() =>
        ToolTip.SetTip(this, "Master-Pegel — rote LED = Übersteuerung (Klick setzt sie zurück)");

    /// <summary>Neue Spitzenpegel (linear 0..1) übernehmen; rechnet in dB und glättet den Abfall.</summary>
    public void SetLevels(float l, float r)
    {
        if (l >= 0.999f || r >= 0.999f) _clip = true;   // 0 dBFS erreicht → LED latcht
        _l = Ease(_l, ToDb01(l));
        _r = Ease(_r, ToDb01(r));
        Track1(ref _peakL, ref _holdL, _l);
        Track1(ref _peakR, ref _holdR, _r);
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _clip = false;                                   // Clip-LED quittieren
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

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (w < 8 || h < 4) return;
        double gap = 2, led = 4;
        double bw = w - led - 2;                        // Balkenbreite; rechts Platz für die Clip-LED
        double bh = (h - gap) / 2;

        DrawBar(dc, 0, bw, bh, _l, _peakL);
        DrawBar(dc, bh + gap, bw, bh, _r, _peakR);

        // Clip-LED: latcht rot bei 0 dBFS, sonst gedimmter Platzhalter (Klick quittiert).
        dc.DrawRectangle(_clip ? ClipLed : Track, null, new RoundedRect(new Rect(w - led, 0, led, h), 1.5));
    }

    private static void DrawBar(DrawingContext dc, double y, double w, double h, float level, float peak)
    {
        dc.DrawRectangle(Track, null, new RoundedRect(new Rect(0, y, w, h), 2));
        if (level > 0.001)
            dc.DrawRectangle(Fill, null, new RoundedRect(new Rect(0, y, w * level, h), 2));
        if (peak > 0.01)
        {
            double x = Math.Min(w - 1, w * peak);
            dc.DrawLine(PeakPen, new Point(x, y + 1), new Point(x, y + h - 1));
        }
    }

    private static IBrush MakeGradient() => new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops =
        [
            new GradientStop(Color.FromRgb(0x3D, 0xDC, 0x84), 0.0),   // grün
            new GradientStop(Color.FromRgb(0x9E, 0xE0, 0x3A), 0.55),
            new GradientStop(Color.FromRgb(0xFF, 0xC2, 0x4B), 0.8),   // gelb
            new GradientStop(Color.FromRgb(0xFF, 0x53, 0x50), 1.0)    // rot
        ]
    };
}
