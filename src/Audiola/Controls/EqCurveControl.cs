using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Audiola.Dsp;
using Audiola.Models;

namespace Audiola.Controls;

/// <summary>
/// Interaktiver EQ: zeichnet Gitter + summierten Frequenzgang der Bänder und lässt
/// die Band-Punkte ziehen (X = Frequenz, log; Y = Gain in dB).
/// </summary>
public sealed class EqCurveControl : FrameworkElement
{
    private const double FMin = 20, FMax = 20000, DbMax = 18, HandleR = 7;

    public static readonly DependencyProperty BandsProperty = DependencyProperty.Register(
        nameof(Bands), typeof(IReadOnlyList<EqBand>), typeof(EqCurveControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnBandsChanged));

    public static readonly DependencyProperty SampleRateProperty = DependencyProperty.Register(
        nameof(SampleRate), typeof(int), typeof(EqCurveControl),
        new FrameworkPropertyMetadata(44100, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<EqBand>? Bands
    {
        get => (IReadOnlyList<EqBand>?)GetValue(BandsProperty);
        set => SetValue(BandsProperty, value);
    }

    public int SampleRate
    {
        get => (int)GetValue(SampleRateProperty);
        set => SetValue(SampleRateProperty, value);
    }

    private EqBand? _drag;

    private static void OnBandsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (EqCurveControl)d;
        if (e.OldValue is IReadOnlyList<EqBand> oldBands)
            foreach (var b in oldBands) b.PropertyChanged -= c.OnBandPropertyChanged;
        if (e.NewValue is IReadOnlyList<EqBand> newBands)
            foreach (var b in newBands) b.PropertyChanged += c.OnBandPropertyChanged;
    }

    private void OnBandPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => InvalidateVisual();

    // ----- Koordinaten-Umrechnung -----
    private double XFromFreq(double f, double w) => Math.Log10(f / FMin) / Math.Log10(FMax / FMin) * w;
    private double FreqFromX(double x, double w) => FMin * Math.Pow(10, x / w * Math.Log10(FMax / FMin));
    private double YFromDb(double db, double h) => (DbMax - db) / (2 * DbMax) * h;
    private double DbFromY(double y, double h) => DbMax - y / h * 2 * DbMax;

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)), 1);
        gridPen.Freeze();
        var zeroPen = new Pen(new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF)), 1);
        zeroPen.Freeze();
        var labelBrush = new SolidColorBrush(Color.FromArgb(0xAA, 0xCC, 0xCC, 0xCC));
        labelBrush.Freeze();
        var typeface = new Typeface("Segoe UI");
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        // Vertikale Frequenz-Linien.
        double[] freqs = [50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000];
        foreach (var f in freqs)
        {
            var x = XFromFreq(f, w);
            dc.DrawLine(gridPen, new Point(x, 0), new Point(x, h));
            if (f is 100 or 1000 or 10000)
            {
                var label = f >= 1000 ? $"{f / 1000:0}k" : $"{f:0}";
                var ft = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 10, labelBrush, dpi);
                dc.DrawText(ft, new Point(x + 2, h - 14));
            }
        }

        // Horizontale dB-Linien.
        foreach (var db in new[] { -12.0, -6, 0, 6, 12 })
        {
            var y = YFromDb(db, h);
            dc.DrawLine(db == 0 ? zeroPen : gridPen, new Point(0, y), new Point(w, y));
            var ft = new FormattedText($"{db:+0;-0;0}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 10, labelBrush, dpi);
            dc.DrawText(ft, new Point(2, y - 14));
        }

        var bands = Bands;
        if (bands is null || bands.Count == 0) return;

        // Filter aus Bändern bauen.
        var filters = bands.Select(b => b.CreateFilter(SampleRate)).ToList();

        // Summierter Frequenzgang als Kurve.
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            var started = false;
            for (var x = 0.0; x <= w; x += 2)
            {
                var f = FreqFromX(x, w);
                var sumDb = filters.Sum(flt => flt.MagnitudeDb(f, SampleRate));
                var y = YFromDb(Math.Clamp(sumDb, -DbMax, DbMax), h);
                var p = new Point(x, y);
                if (!started) { ctx.BeginFigure(p, false, false); started = true; }
                else ctx.LineTo(p, true, false);
            }
        }
        geo.Freeze();
        var curvePen = new Pen(new SolidColorBrush(Color.FromRgb(0x9E, 0xC0, 0xFF)), 2);
        curvePen.Freeze();
        dc.DrawGeometry(null, curvePen, geo);

        // Band-Punkte.
        foreach (var b in bands)
        {
            var center = new Point(XFromFreq(b.Frequency, w), YFromDb(b.GainDb, h));
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(b.ColorHex));
            brush.Freeze();
            dc.DrawEllipse(brush, new Pen(Brushes.White, 1.5), center, HandleR, HandleR);
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        var bands = Bands;
        if (bands is null) return;
        var pos = e.GetPosition(this);

        _drag = bands
            .OrderBy(b => Dist(b, pos))
            .FirstOrDefault(b => Dist(b, pos) <= HandleR * 2.5);

        if (_drag is not null)
        {
            CaptureMouse();
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_drag is null) return;
        double w = ActualWidth, h = ActualHeight;
        var pos = e.GetPosition(this);

        _drag.Frequency = Math.Clamp(FreqFromX(pos.X, w), FMin, FMax);
        _drag.GainDb = Math.Clamp(DbFromY(pos.Y, h), -DbMax, DbMax);
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (_drag is not null)
        {
            _drag = null;
            ReleaseMouseCapture();
        }
    }

    private double Dist(EqBand b, Point p)
    {
        double w = ActualWidth, h = ActualHeight;
        var dx = XFromFreq(b.Frequency, w) - p.X;
        var dy = YFromDb(b.GainDb, h) - p.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
