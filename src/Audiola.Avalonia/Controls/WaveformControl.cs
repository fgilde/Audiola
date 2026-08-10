using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Audiola.Controls;

/// <summary>
/// Zeichnet eine Wellenform aus Min/Max-Peaks (zwei Werte pro Bucket, Bereich [-1,1])
/// und markiert den bereits abgespielten Bereich. Gleiche Eigenschaften wie die
/// WPF-Fassung, damit die portierten Views unverändert binden.
/// </summary>
public sealed class WaveformControl : Control
{
    private static readonly IBrush DefaultWaveBrush = new SolidColorBrush(Color.FromRgb(0x5B, 0x8C, 0xFF));
    private static readonly IBrush DefaultPlayedBrush = new SolidColorBrush(Color.FromRgb(0x9E, 0xC0, 0xFF));
    private static readonly IBrush SelectionFill = new SolidColorBrush(Color.FromArgb(0x40, 0x5B, 0x8C, 0xFF));
    private static readonly IPen SelectionEdge = new Pen(new SolidColorBrush(Color.FromArgb(0xC0, 0x9E, 0xC0, 0xFF)), 1);

    public static readonly StyledProperty<IReadOnlyList<float>?> PeaksProperty =
        AvaloniaProperty.Register<WaveformControl, IReadOnlyList<float>?>(nameof(Peaks));

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<WaveformControl, double>(nameof(Progress));

    public static readonly StyledProperty<IBrush> WaveBrushProperty =
        AvaloniaProperty.Register<WaveformControl, IBrush>(nameof(WaveBrush), DefaultWaveBrush);

    public static readonly StyledProperty<IBrush> PlayedBrushProperty =
        AvaloniaProperty.Register<WaveformControl, IBrush>(nameof(PlayedBrush), DefaultPlayedBrush);

    public static readonly StyledProperty<double> SelectionStartProperty =
        AvaloniaProperty.Register<WaveformControl, double>(nameof(SelectionStart), double.NaN);

    public static readonly StyledProperty<double> SelectionEndProperty =
        AvaloniaProperty.Register<WaveformControl, double>(nameof(SelectionEnd), double.NaN);

    static WaveformControl() =>
        AffectsRender<WaveformControl>(PeaksProperty, ProgressProperty, WaveBrushProperty,
            PlayedBrushProperty, SelectionStartProperty, SelectionEndProperty);

    public IReadOnlyList<float>? Peaks
    {
        get => GetValue(PeaksProperty);
        set => SetValue(PeaksProperty, value);
    }

    /// <summary>Wiedergabefortschritt 0..1.</summary>
    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public IBrush WaveBrush
    {
        get => GetValue(WaveBrushProperty);
        set => SetValue(WaveBrushProperty, value);
    }

    public IBrush PlayedBrush
    {
        get => GetValue(PlayedBrushProperty);
        set => SetValue(PlayedBrushProperty, value);
    }

    /// <summary>Auswahlbeginn als Verhältnis 0..1 (NaN = keine Auswahl).</summary>
    public double SelectionStart
    {
        get => GetValue(SelectionStartProperty);
        set => SetValue(SelectionStartProperty, value);
    }

    /// <summary>Auswahlende als Verhältnis 0..1 (NaN = keine Auswahl).</summary>
    public double SelectionEnd
    {
        get => GetValue(SelectionEndProperty);
        set => SetValue(SelectionEndProperty, value);
    }

    public override void Render(DrawingContext dc)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0) return;

        // Hintergrund (transparent klickbar machen).
        dc.FillRectangle(Brushes.Transparent, new Rect(0, 0, width, height));

        // Auswahl-Markierung.
        var selStart = SelectionStart;
        var selEnd = SelectionEnd;
        if (!double.IsNaN(selStart) && !double.IsNaN(selEnd) && selEnd > selStart)
        {
            var xA = width * Math.Clamp(selStart, 0, 1);
            var xB = width * Math.Clamp(selEnd, 0, 1);
            dc.FillRectangle(SelectionFill, new Rect(xA, 0, xB - xA, height));
            dc.DrawLine(SelectionEdge, new Point(xA, 0), new Point(xA, height));
            dc.DrawLine(SelectionEdge, new Point(xB, 0), new Point(xB, height));
        }

        var peaks = Peaks;
        if (peaks is null || peaks.Count < 2) return;

        var bucketCount = peaks.Count / 2;
        var midY = height / 2.0;
        var playedX = width * Math.Clamp(Progress, 0, 1);

        var wavePen = new Pen(WaveBrush, 1.0);
        var playedPen = new Pen(PlayedBrush, 1.0);

        for (var x = 0; x < (int)width; x++)
        {
            // Pixel x -> Bucket.
            var bucket = (int)(x / width * bucketCount);
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
