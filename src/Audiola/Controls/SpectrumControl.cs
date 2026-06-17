using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Audiola.Controls;

/// <summary>
/// audioMotion-artiger Balken-Visualizer (Werte 0..1). Daten kommen im Wiedergabe-Takt
/// per <see cref="SetLevels"/>; gezeichnet wird frame-synchron mit Easing, frequenz-
/// gefärbtem Gradient, Peak-Hold-Kappen und einem Neon-Glow.
/// </summary>
public sealed class SpectrumControl : FrameworkElement
{
    private float[] _targets = [];
    private float[] _current = [];
    private float[] _peaks = [];
    private readonly Brush _gradient;
    private readonly Brush _capBrush;
    private bool _hooked;

    public SpectrumControl()
    {
        // Frequenz-Gradient quer über die Breite (tief → hoch): Blau → Cyan → Magenta.
        var g = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        g.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#3F8CFF"), 0.0));
        g.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#6BD6FF"), 0.45));
        g.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#B56BFF"), 1.0));
        g.Freeze();
        _gradient = g;

        var cap = new SolidColorBrush(Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF));
        cap.Freeze();
        _capBrush = cap;

        IsHitTestVisible = false;
        Effect = new DropShadowEffect
        {
            Color = (Color)ColorConverter.ConvertFromString("#6BD6FF"),
            BlurRadius = 7,
            ShadowDepth = 0,
            Opacity = 0.7
        };

        Loaded += (_, _) => Hook();
        Unloaded += (_, _) => Unhook();
    }

    /// <summary>Neue Zielpegel (0..1) setzen — werden weich angefahren.</summary>
    public void SetLevels(float[] levels)
    {
        if (_targets.Length != levels.Length) _targets = new float[levels.Length];
        Array.Copy(levels, _targets, levels.Length);
    }

    private void Hook() { if (!_hooked) { CompositionTarget.Rendering += OnFrame; _hooked = true; } }
    private void Unhook() { if (_hooked) { CompositionTarget.Rendering -= OnFrame; _hooked = false; } }

    private void OnFrame(object? sender, EventArgs e)
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

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
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
            bars.Children.Add(new RectangleGeometry(new Rect(x, h - bh, barW, bh), 1.2, 1.2));
        }
        if (bars.Children.Count > 0)
        {
            bars.Freeze();
            dc.PushClip(bars);
            dc.DrawRectangle(_gradient, null, new Rect(0, 0, w, h));
            dc.Pop();
        }

        // Peak-Hold-Kappen.
        for (var i = 0; i < n; i++)
        {
            var pk = Math.Clamp(_peaks[i], 0f, 1f);
            if (pk < 0.02f) continue;
            var x = i * (barW + gap);
            var py = h - pk * h;
            dc.DrawRectangle(_capBrush, null, new Rect(x, Math.Max(0, py - 1.5), barW, 1.5));
        }
    }
}
