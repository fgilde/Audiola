using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Audiola.Controls;

/// <summary>
/// audioMotion-artiger Balken-Visualizer (Werte 0..1). Daten kommen im Wiedergabe-Takt
/// per <see cref="SetLevels"/>; gezeichnet wird frame-synchron mit Easing, frequenz-
/// gefärbtem Gradient, Peak-Hold-Kappen und einem Neon-Glow.
/// </summary>
public sealed class SpectrumControl : Control
{
    private readonly IBrush _gradient;
    private readonly IBrush _capBrush = new SolidColorBrush(Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF));
    private readonly DispatcherTimer _frames;
    private float[] _targets = [];
    private float[] _current = [];
    private float[] _peaks = [];

    public SpectrumControl()
    {
        // Frequenz-Gradient quer über die Breite (tief → hoch): Blau → Cyan → Magenta.
        _gradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops =
            [
                new GradientStop(Color.Parse("#3F8CFF"), 0.0),
                new GradientStop(Color.Parse("#6BD6FF"), 0.45),
                new GradientStop(Color.Parse("#B56BFF"), 1.0)
            ]
        };

        IsHitTestVisible = false;
        Effect = new DropShadowEffect
        {
            Color = Color.Parse("#6BD6FF"),
            BlurRadius = 7,
            OffsetX = 0,
            OffsetY = 0,
            Opacity = 0.7
        };

        // Avalonia hat kein CompositionTarget.Rendering — ein Render-Timer erledigt dasselbe.
        _frames = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, (_, _) => OnFrame());
        AttachedToVisualTree += (_, _) => _frames.Start();
        DetachedFromVisualTree += (_, _) => _frames.Stop();
    }

    /// <summary>Neue Zielpegel (0..1) setzen — werden weich angefahren.</summary>
    public void SetLevels(float[] levels)
    {
        if (_targets.Length != levels.Length) _targets = new float[levels.Length];
        Array.Copy(levels, _targets, levels.Length);
    }

    private void OnFrame()
    {
        var n = _targets.Length;
        if (n == 0) return;
        if (_current.Length != n) { _current = new float[n]; _peaks = new float[n]; }

        var changed = false;
        for (var i = 0; i < n; i++)
        {
            var t = _targets[i];
            var c = _current[i];
            var nc = c + (t - c) * (t > c ? 0.55f : 0.20f);   // schnell rauf, weich runter
            if (MathF.Abs(nc - c) > 0.0015f) changed = true;
            _current[i] = nc;

            // Peak-Hold: hält den Spitzenwert kurz und fällt dann langsam.
            if (nc >= _peaks[i]) { _peaks[i] = nc; }
            else { _peaks[i] = MathF.Max(nc, _peaks[i] - 0.012f); changed = true; }
        }
        if (changed) InvalidateVisual();
    }

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width, h = Bounds.Height;
        var n = _current.Length;
        if (n == 0 || w <= 0 || h <= 0) return;

        const double gap = 2;
        var barW = Math.Max(1.0, (w - gap * (n - 1)) / n);

        // Alle Balken als Clip-Geometrie, dann mit dem Frequenz-Gradient füllen → Farbe je nach Position.
        var bars = new GeometryGroup();
        for (var i = 0; i < n; i++)
        {
            var lv = Math.Clamp(_current[i], 0f, 1f);
            var bh = lv * h;
            if (bh < 1) continue;
            var x = i * (barW + gap);
            bars.Children.Add(new RectangleGeometry(new Rect(x, h - bh, barW, bh)));
        }
        if (bars.Children.Count > 0)
        {
            using (dc.PushGeometryClip(bars))
                dc.FillRectangle(_gradient, new Rect(0, 0, w, h));
        }

        // Peak-Hold-Kappen.
        for (var i = 0; i < n; i++)
        {
            var pk = Math.Clamp(_peaks[i], 0f, 1f);
            if (pk < 0.02f) continue;
            var x = i * (barW + gap);
            var py = h - pk * h;
            dc.FillRectangle(_capBrush, new Rect(x, Math.Max(0, py - 1.5), barW, 1.5));
        }
    }
}
