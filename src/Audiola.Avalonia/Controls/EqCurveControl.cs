using System.Globalization;
using Audiola.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Audiola.Controls;

/// <summary>
/// Interaktiver EQ: zeichnet Gitter + summierten Frequenzgang der Bänder und lässt
/// die Band-Punkte ziehen (X = Frequenz, log; Y = Gain in dB).
/// </summary>
public sealed class EqCurveControl : Control
{
    private const double FMin = 20, FMax = 20000, DbMax = 18, HandleR = 7;

    private static readonly IPen GridPen = new Pen(new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)), 1);
    private static readonly IPen ZeroPen = new Pen(new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF)), 1);
    private static readonly IBrush LabelBrush = new SolidColorBrush(Color.FromArgb(0xAA, 0xCC, 0xCC, 0xCC));
    private static readonly IPen CurvePen = new Pen(new SolidColorBrush(Color.FromRgb(0x9E, 0xC0, 0xFF)), 2);
    private static readonly IPen HandlePen = new Pen(Brushes.White, 1.5);
    private static readonly Typeface LabelTypeface = new("Segoe UI");

    public static readonly StyledProperty<IReadOnlyList<EqBand>?> BandsProperty =
        AvaloniaProperty.Register<EqCurveControl, IReadOnlyList<EqBand>?>(nameof(Bands));

    public static readonly StyledProperty<int> SampleRateProperty =
        AvaloniaProperty.Register<EqCurveControl, int>(nameof(SampleRate), 44100);

    private EqBand? _drag;

    static EqCurveControl()
    {
        AffectsRender<EqCurveControl>(BandsProperty, SampleRateProperty);
        BandsProperty.Changed.AddClassHandler<EqCurveControl, IReadOnlyList<EqBand>?>((c, e) => c.OnBandsChanged(e));
    }

    public IReadOnlyList<EqBand>? Bands
    {
        get => GetValue(BandsProperty);
        set => SetValue(BandsProperty, value);
    }

    public int SampleRate
    {
        get => GetValue(SampleRateProperty);
        set => SetValue(SampleRateProperty, value);
    }

    private void OnBandsChanged(AvaloniaPropertyChangedEventArgs<IReadOnlyList<EqBand>?> e)
    {
        if (e.OldValue.GetValueOrDefault() is { } oldBands)
            foreach (var b in oldBands) b.PropertyChanged -= OnBandPropertyChanged;
        if (e.NewValue.GetValueOrDefault() is { } newBands)
            foreach (var b in newBands) b.PropertyChanged += OnBandPropertyChanged;
    }

    private void OnBandPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => InvalidateVisual();

    // ----- Koordinaten-Umrechnung -----
    private static double XFromFreq(double f, double w) => Math.Log10(f / FMin) / Math.Log10(FMax / FMin) * w;
    private static double FreqFromX(double x, double w) => FMin * Math.Pow(10, x / w * Math.Log10(FMax / FMin));
    private static double YFromDb(double db, double h) => (DbMax - db) / (2 * DbMax) * h;
    private static double DbFromY(double y, double h) => DbMax - y / h * 2 * DbMax;

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        dc.FillRectangle(Brushes.Transparent, new Rect(0, 0, w, h));

        // Vertikale Frequenz-Linien.
        double[] freqs = [50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000];
        foreach (var f in freqs)
        {
            var x = XFromFreq(f, w);
            dc.DrawLine(GridPen, new Point(x, 0), new Point(x, h));
            if (f is 100 or 1000 or 10000)
            {
                var label = f >= 1000 ? $"{f / 1000:0}k" : $"{f:0}";
                dc.DrawText(Text(label), new Point(x + 2, h - 14));
            }
        }

        // Horizontale dB-Linien.
        foreach (var db in new[] { -12.0, -6, 0, 6, 12 })
        {
            var y = YFromDb(db, h);
            dc.DrawLine(db == 0 ? ZeroPen : GridPen, new Point(0, y), new Point(w, y));
            dc.DrawText(Text($"{db:+0;-0;0}"), new Point(2, y - 14));
        }

        var bands = Bands;
        if (bands is null || bands.Count == 0) return;

        // Filter aus Bändern bauen.
        var filters = bands.Select(b => b.CreateFilter(SampleRate)).ToList();

        // Summierter Frequenzgang als Kurve.
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var started = false;
            for (var x = 0.0; x <= w; x += 2)
            {
                var f = FreqFromX(x, w);
                var sumDb = filters.Sum(flt => flt.MagnitudeDb(f, SampleRate));
                var p = new Point(x, YFromDb(Math.Clamp(sumDb, -DbMax, DbMax), h));
                if (!started) { ctx.BeginFigure(p, false); started = true; }
                else ctx.LineTo(p);
            }
            if (started) ctx.EndFigure(false);
        }
        dc.DrawGeometry(null, CurvePen, geometry);

        // Band-Punkte.
        foreach (var b in bands)
        {
            var center = new Point(XFromFreq(b.Frequency, w), YFromDb(b.GainDb, h));
            dc.DrawEllipse(new SolidColorBrush(Color.Parse(b.ColorHex)), HandlePen, center, HandleR, HandleR);
        }
    }

    private static FormattedText Text(string value) =>
        new(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, LabelTypeface, 10, LabelBrush);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var bands = Bands;
        if (bands is null) return;
        var pos = e.GetPosition(this);

        _drag = bands
            .OrderBy(b => Dist(b, pos))
            .FirstOrDefault(b => Dist(b, pos) <= HandleR * 2.5);

        if (_drag is not null)
        {
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_drag is null) return;
        double w = Bounds.Width, h = Bounds.Height;
        var pos = e.GetPosition(this);

        _drag.Frequency = Math.Clamp(FreqFromX(pos.X, w), FMin, FMax);
        _drag.GainDb = Math.Clamp(DbFromY(pos.Y, h), -DbMax, DbMax);
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_drag is not null)
        {
            _drag = null;
            e.Pointer.Capture(null);
        }
    }

    private double Dist(EqBand b, Point p)
    {
        double w = Bounds.Width, h = Bounds.Height;
        var dx = XFromFreq(b.Frequency, w) - p.X;
        var dy = YFromDb(b.GainDb, h) - p.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
