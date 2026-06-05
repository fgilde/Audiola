using System.Windows;
using System.Windows.Media;

namespace Audiola.Controls;

/// <summary>
/// Zeichnet eine Wellenform aus Min/Max-Peaks (zwei Werte pro Bucket, Bereich [-1,1])
/// und markiert den bereits abgespielten Bereich.
/// </summary>
public sealed class WaveformControl : FrameworkElement
{
    public static readonly DependencyProperty PeaksProperty = DependencyProperty.Register(
        nameof(Peaks), typeof(IReadOnlyList<float>), typeof(WaveformControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(WaveformControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty WaveBrushProperty = DependencyProperty.Register(
        nameof(WaveBrush), typeof(Brush), typeof(WaveformControl),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x5B, 0x8C, 0xFF)),
            FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PlayedBrushProperty = DependencyProperty.Register(
        nameof(PlayedBrush), typeof(Brush), typeof(WaveformControl),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x9E, 0xC0, 0xFF)),
            FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SelectionStartProperty = DependencyProperty.Register(
        nameof(SelectionStart), typeof(double), typeof(WaveformControl),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SelectionEndProperty = DependencyProperty.Register(
        nameof(SelectionEnd), typeof(double), typeof(WaveformControl),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<float>? Peaks
    {
        get => (IReadOnlyList<float>?)GetValue(PeaksProperty);
        set => SetValue(PeaksProperty, value);
    }

    /// <summary>Wiedergabefortschritt 0..1.</summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public Brush WaveBrush
    {
        get => (Brush)GetValue(WaveBrushProperty);
        set => SetValue(WaveBrushProperty, value);
    }

    public Brush PlayedBrush
    {
        get => (Brush)GetValue(PlayedBrushProperty);
        set => SetValue(PlayedBrushProperty, value);
    }

    /// <summary>Auswahlbeginn als Verhältnis 0..1 (NaN = keine Auswahl).</summary>
    public double SelectionStart
    {
        get => (double)GetValue(SelectionStartProperty);
        set => SetValue(SelectionStartProperty, value);
    }

    /// <summary>Auswahlende als Verhältnis 0..1 (NaN = keine Auswahl).</summary>
    public double SelectionEnd
    {
        get => (double)GetValue(SelectionEndProperty);
        set => SetValue(SelectionEndProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0) return;

        // Hintergrund (transparent klickbar machen).
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));

        // Auswahl-Markierung.
        var selStart = SelectionStart;
        var selEnd = SelectionEnd;
        if (!double.IsNaN(selStart) && !double.IsNaN(selEnd) && selEnd > selStart)
        {
            var xA = width * Math.Clamp(selStart, 0, 1);
            var xB = width * Math.Clamp(selEnd, 0, 1);
            var fill = new SolidColorBrush(Color.FromArgb(0x40, 0x5B, 0x8C, 0xFF));
            fill.Freeze();
            var edge = new Pen(new SolidColorBrush(Color.FromArgb(0xC0, 0x9E, 0xC0, 0xFF)), 1);
            edge.Freeze();
            dc.DrawRectangle(fill, null, new Rect(xA, 0, xB - xA, height));
            dc.DrawLine(edge, new Point(xA, 0), new Point(xA, height));
            dc.DrawLine(edge, new Point(xB, 0), new Point(xB, height));
        }

        var peaks = Peaks;
        if (peaks is null || peaks.Count < 2) return;

        var bucketCount = peaks.Count / 2;
        var midY = height / 2.0;
        var playedX = width * Math.Clamp(Progress, 0, 1);

        var wavePen = new Pen(WaveBrush, 1.0);
        var playedPen = new Pen(PlayedBrush, 1.0);
        wavePen.Freeze();
        playedPen.Freeze();

        for (var x = 0; x < (int)width; x++)
        {
            // Pixel x -> Bucket.
            var bucket = (int)((double)x / width * bucketCount);
            if (bucket >= bucketCount) bucket = bucketCount - 1;

            var min = peaks[bucket * 2];
            var max = peaks[bucket * 2 + 1];

            var yMax = midY - max * midY;
            var yMin = midY - min * midY;

            // Mindesthoehe fuer Sichtbarkeit.
            if (Math.Abs(yMin - yMax) < 1) yMin = yMax + 1;

            var pen = x <= playedX ? playedPen : wavePen;
            dc.DrawLine(pen, new Point(x + 0.5, yMax), new Point(x + 0.5, yMin));
        }
    }
}
