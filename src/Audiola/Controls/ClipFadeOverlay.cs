using System.Windows;
using System.Windows.Media;

namespace Audiola.Controls;

/// <summary>Zeichnet die Fade-In/Fade-Out-Rampen über einem Clip (abgedunkelte Ecke + Linie).</summary>
public sealed class ClipFadeOverlay : FrameworkElement
{
    public static readonly DependencyProperty FadeInSecondsProperty = DependencyProperty.Register(
        nameof(FadeInSeconds), typeof(double), typeof(ClipFadeOverlay),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FadeOutSecondsProperty = DependencyProperty.Register(
        nameof(FadeOutSeconds), typeof(double), typeof(ClipFadeOverlay),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PixelsPerSecondProperty = DependencyProperty.Register(
        nameof(PixelsPerSecond), typeof(double), typeof(ClipFadeOverlay),
        new FrameworkPropertyMetadata(40.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double FadeInSeconds { get => (double)GetValue(FadeInSecondsProperty); set => SetValue(FadeInSecondsProperty, value); }
    public double FadeOutSeconds { get => (double)GetValue(FadeOutSecondsProperty); set => SetValue(FadeOutSecondsProperty, value); }
    public double PixelsPerSecond { get => (double)GetValue(PixelsPerSecondProperty); set => SetValue(PixelsPerSecondProperty, value); }

    private static readonly Brush Shade = CreateShade();
    private static readonly Pen Line = CreateLine();

    private static Brush CreateShade() { var b = new SolidColorBrush(Color.FromArgb(0x66, 0, 0, 0)); b.Freeze(); return b; }
    private static Pen CreateLine() { var p = new Pen(new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)), 1.2); p.Freeze(); return p; }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var inPx = Math.Min(FadeInSeconds * PixelsPerSecond, w);
        if (inPx > 1)
        {
            var geo = Triangle(new Point(0, 0), new Point(inPx, 0), new Point(0, h));
            dc.DrawGeometry(Shade, null, geo);
            dc.DrawLine(Line, new Point(0, h), new Point(inPx, 0));
        }

        var outPx = Math.Min(FadeOutSeconds * PixelsPerSecond, w);
        if (outPx > 1)
        {
            var geo = Triangle(new Point(w, 0), new Point(w - outPx, 0), new Point(w, h));
            dc.DrawGeometry(Shade, null, geo);
            dc.DrawLine(Line, new Point(w, h), new Point(w - outPx, 0));
        }
    }

    private static Geometry Triangle(Point a, Point b, Point c)
    {
        var fig = new PathFigure { StartPoint = a, IsClosed = true };
        fig.Segments.Add(new LineSegment(b, false));
        fig.Segments.Add(new LineSegment(c, false));
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        geo.Freeze();
        return geo;
    }
}
