using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Audiola.Controls;

/// <summary>
/// Zeit-Lineal für die Timeline: zeichnet Ticks + mm:ss-Beschriftungen entlang der
/// Zeitachse. Die Breite ergibt sich aus Dauer × Pixel/Sekunde (Measure).
/// </summary>
public sealed class TimelineRulerControl : FrameworkElement
{
    private static readonly double[] Steps = [0.1, 0.25, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600];

    public static readonly DependencyProperty PixelsPerSecondProperty = DependencyProperty.Register(
        nameof(PixelsPerSecond), typeof(double), typeof(TimelineRulerControl),
        new FrameworkPropertyMetadata(40.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty DurationSecondsProperty = DependencyProperty.Register(
        nameof(DurationSeconds), typeof(double), typeof(TimelineRulerControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public double PixelsPerSecond
    {
        get => (double)GetValue(PixelsPerSecondProperty);
        set => SetValue(PixelsPerSecondProperty, value);
    }

    public double DurationSeconds
    {
        get => (double)GetValue(DurationSecondsProperty);
        set => SetValue(DurationSecondsProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
        => new(Math.Max(0, DurationSeconds * PixelsPerSecond), 28);

    protected override void OnRender(DrawingContext dc)
    {
        var pps = PixelsPerSecond;
        var dur = DurationSeconds;
        var height = 28.0;
        var width = dur * pps;
        if (pps <= 0 || dur <= 0) return;

        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));

        var tickBrush = new SolidColorBrush(Color.FromArgb(0x88, 0x9E, 0xC0, 0xFF));
        tickBrush.Freeze();
        var labelBrush = new SolidColorBrush(Color.FromArgb(0xCC, 0xCC, 0xCC, 0xCC));
        labelBrush.Freeze();
        var pen = new Pen(tickBrush, 1);
        pen.Freeze();

        // Tick-Abstand so wählen, dass ~70 px Mindestabstand entstehen.
        var interval = Steps[^1];
        foreach (var s in Steps)
        {
            if (s * pps >= 70) { interval = s; break; }
        }

        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface("Segoe UI");

        for (var t = 0.0; t <= dur + 1e-6; t += interval)
        {
            var x = t * pps;
            dc.DrawLine(pen, new Point(x, height - 8), new Point(x, height));

            var label = Format(t, interval);
            var ft = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                typeface, 10, labelBrush, dpi);
            dc.DrawText(ft, new Point(x + 3, 4));
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
