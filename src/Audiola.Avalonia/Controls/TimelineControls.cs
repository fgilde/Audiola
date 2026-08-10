using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Audiola.Controls;

/// <summary>Zeichnet feine vertikale Rasterlinien im Snap-Abstand hinter den Spuren.</summary>
public sealed class TimelineGridControl : Control
{
    private static readonly IPen GridPen = new Pen(new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF)), 1);

    public static readonly StyledProperty<double> PixelsPerSecondProperty =
        AvaloniaProperty.Register<TimelineGridControl, double>(nameof(PixelsPerSecond), 40.0);

    public static readonly StyledProperty<double> GridSecondsProperty =
        AvaloniaProperty.Register<TimelineGridControl, double>(nameof(GridSeconds), 0.25);

    static TimelineGridControl() =>
        AffectsRender<TimelineGridControl>(PixelsPerSecondProperty, GridSecondsProperty);

    public double PixelsPerSecond
    {
        get => GetValue(PixelsPerSecondProperty);
        set => SetValue(PixelsPerSecondProperty, value);
    }

    public double GridSeconds
    {
        get => GetValue(GridSecondsProperty);
        set => SetValue(GridSecondsProperty, value);
    }

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var step = GridSeconds * PixelsPerSecond;
        if (step < 4) return; // zu eng -> nicht zeichnen

        for (var x = 0.0; x <= w; x += step)
            dc.DrawLine(GridPen, new Point(x, 0), new Point(x, h));
    }
}

/// <summary>Zeichnet die Fade-In/Fade-Out-Rampen über einem Clip (abgedunkelte Ecke + Linie).</summary>
public sealed class ClipFadeOverlay : Control
{
    private static readonly IBrush Shade = new SolidColorBrush(Color.FromArgb(0x66, 0, 0, 0));
    private static readonly IPen Line = new Pen(new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)), 1.2);

    public static readonly StyledProperty<double> FadeInSecondsProperty =
        AvaloniaProperty.Register<ClipFadeOverlay, double>(nameof(FadeInSeconds));

    public static readonly StyledProperty<double> FadeOutSecondsProperty =
        AvaloniaProperty.Register<ClipFadeOverlay, double>(nameof(FadeOutSeconds));

    public static readonly StyledProperty<double> PixelsPerSecondProperty =
        AvaloniaProperty.Register<ClipFadeOverlay, double>(nameof(PixelsPerSecond), 40.0);

    static ClipFadeOverlay() =>
        AffectsRender<ClipFadeOverlay>(FadeInSecondsProperty, FadeOutSecondsProperty, PixelsPerSecondProperty);

    public double FadeInSeconds { get => GetValue(FadeInSecondsProperty); set => SetValue(FadeInSecondsProperty, value); }
    public double FadeOutSeconds { get => GetValue(FadeOutSecondsProperty); set => SetValue(FadeOutSecondsProperty, value); }
    public double PixelsPerSecond { get => GetValue(PixelsPerSecondProperty); set => SetValue(PixelsPerSecondProperty, value); }

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var inPx = Math.Min(FadeInSeconds * PixelsPerSecond, w);
        if (inPx > 1)
        {
            dc.DrawGeometry(Shade, null, Triangle(new Point(0, 0), new Point(inPx, 0), new Point(0, h)));
            dc.DrawLine(Line, new Point(0, h), new Point(inPx, 0));
        }

        var outPx = Math.Min(FadeOutSeconds * PixelsPerSecond, w);
        if (outPx > 1)
        {
            dc.DrawGeometry(Shade, null, Triangle(new Point(w, 0), new Point(w - outPx, 0), new Point(w, h)));
            dc.DrawLine(Line, new Point(w, h), new Point(w - outPx, 0));
        }
    }

    private static Geometry Triangle(Point a, Point b, Point c)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(a, true);
        ctx.LineTo(b);
        ctx.LineTo(c);
        ctx.EndFigure(true);
        return geometry;
    }
}

/// <summary>
/// Zeit-Lineal für die Timeline: zeichnet Ticks + mm:ss-Beschriftungen entlang der
/// Zeitachse. Die Breite ergibt sich aus Dauer × Pixel/Sekunde (Measure).
/// </summary>
public sealed class TimelineRulerControl : Control
{
    private const double RulerHeight = 28.0;
    private static readonly double[] Steps = [0.1, 0.25, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600];
    private static readonly IBrush TickBrush = new SolidColorBrush(Color.FromArgb(0x88, 0x9E, 0xC0, 0xFF));
    private static readonly IBrush LabelBrush = new SolidColorBrush(Color.FromArgb(0xCC, 0xCC, 0xCC, 0xCC));
    private static readonly IPen TickPen = new Pen(TickBrush, 1);
    private static readonly Typeface LabelTypeface = new("Segoe UI");

    public static readonly StyledProperty<double> PixelsPerSecondProperty =
        AvaloniaProperty.Register<TimelineRulerControl, double>(nameof(PixelsPerSecond), 40.0);

    public static readonly StyledProperty<double> DurationSecondsProperty =
        AvaloniaProperty.Register<TimelineRulerControl, double>(nameof(DurationSeconds));

    static TimelineRulerControl()
    {
        AffectsRender<TimelineRulerControl>(PixelsPerSecondProperty, DurationSecondsProperty);
        AffectsMeasure<TimelineRulerControl>(PixelsPerSecondProperty, DurationSecondsProperty);
    }

    public double PixelsPerSecond
    {
        get => GetValue(PixelsPerSecondProperty);
        set => SetValue(PixelsPerSecondProperty, value);
    }

    public double DurationSeconds
    {
        get => GetValue(DurationSecondsProperty);
        set => SetValue(DurationSecondsProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
        => new(Math.Max(0, DurationSeconds * PixelsPerSecond), RulerHeight);

    public override void Render(DrawingContext dc)
    {
        var pps = PixelsPerSecond;
        var dur = DurationSeconds;
        var width = dur * pps;
        if (pps <= 0 || dur <= 0) return;

        dc.FillRectangle(Brushes.Transparent, new Rect(0, 0, width, RulerHeight));

        // Tick-Abstand so wählen, dass ~70 px Mindestabstand entstehen.
        var interval = Steps[^1];
        foreach (var s in Steps)
        {
            if (s * pps >= 70) { interval = s; break; }
        }

        for (var t = 0.0; t <= dur + 1e-6; t += interval)
        {
            var x = t * pps;
            dc.DrawLine(TickPen, new Point(x, RulerHeight - 8), new Point(x, RulerHeight));

            var text = new FormattedText(Format(t, interval), CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, LabelTypeface, 10, LabelBrush);
            dc.DrawText(text, new Point(x + 3, 4));
        }
    }

    private static string Format(double seconds, double interval)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return interval < 1
            ? $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 100}"
            : $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
    }
}
