using System.Windows;
using System.Windows.Media;

namespace Audiola.Controls;

/// <summary>Zeichnet feine vertikale Rasterlinien im Snap-Abstand hinter den Spuren.</summary>
public sealed class TimelineGridControl : FrameworkElement
{
    public static readonly DependencyProperty PixelsPerSecondProperty = DependencyProperty.Register(
        nameof(PixelsPerSecond), typeof(double), typeof(TimelineGridControl),
        new FrameworkPropertyMetadata(40.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty GridSecondsProperty = DependencyProperty.Register(
        nameof(GridSeconds), typeof(double), typeof(TimelineGridControl),
        new FrameworkPropertyMetadata(0.25, FrameworkPropertyMetadataOptions.AffectsRender));

    public double PixelsPerSecond
    {
        get => (double)GetValue(PixelsPerSecondProperty);
        set => SetValue(PixelsPerSecondProperty, value);
    }

    public double GridSeconds
    {
        get => (double)GetValue(GridSecondsProperty);
        set => SetValue(GridSecondsProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var step = GridSeconds * PixelsPerSecond;
        if (step < 4) return; // zu eng -> nicht zeichnen

        var pen = new Pen(new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF)), 1);
        pen.Freeze();

        for (var x = 0.0; x <= w; x += step)
            dc.DrawLine(pen, new Point(x, 0), new Point(x, h));
    }
}
