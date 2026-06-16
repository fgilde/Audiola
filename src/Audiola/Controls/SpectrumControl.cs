using System.Windows;
using System.Windows.Media;

namespace Audiola.Controls;

/// <summary>
/// Balken-Spektrum (Werte 0..1). Die Daten kommen im Wiedergabe-Takt (~20 Hz) per
/// <see cref="SetLevels"/>; gezeichnet wird frame-synchron (CompositionTarget.Rendering)
/// mit Easing, damit es flüssig statt ruckelig läuft.
/// </summary>
public sealed class SpectrumControl : FrameworkElement
{
    private float[] _targets = [];
    private float[] _current = [];
    private readonly Brush _barBrush;
    private bool _hooked;

    public SpectrumControl()
    {
        var b = new LinearGradientBrush { StartPoint = new Point(0, 1), EndPoint = new Point(0, 0) };
        b.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#2F5FCF"), 0));
        b.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#6BD6FF"), 1));
        b.Freeze();
        _barBrush = b;
        IsHitTestVisible = false;

        Loaded += (_, _) => Hook();
        Unloaded += (_, _) => Unhook();
    }

    /// <summary>Neue Zielpegel (0..1) setzen — wird weich angefahren.</summary>
    public void SetLevels(float[] levels)
    {
        if (_targets.Length != levels.Length) _targets = new float[levels.Length];
        Array.Copy(levels, _targets, levels.Length);
    }

    private void Hook()
    {
        if (_hooked) return;
        CompositionTarget.Rendering += OnFrame;
        _hooked = true;
    }

    private void Unhook()
    {
        if (!_hooked) return;
        CompositionTarget.Rendering -= OnFrame;
        _hooked = false;
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        if (_targets.Length == 0) return;
        if (_current.Length != _targets.Length) _current = new float[_targets.Length];

        var changed = false;
        for (var i = 0; i < _current.Length; i++)
        {
            var t = _targets[i];
            var c = _current[i];
            // schnell rauf, weich runter
            var nc = c + (t - c) * (t > c ? 0.55f : 0.20f);
            if (MathF.Abs(nc - c) > 0.0015f) changed = true;
            _current[i] = nc;
        }
        if (changed) InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        var n = _current.Length;
        if (n < 2 || w <= 0 || h <= 0) return;

        // Glatte, gespiegelte Fläche um die Mittellinie — wirkt flüssiger als Einzelbalken
        // und skaliert sauber, auch wenn das Element nur sehr klein ist.
        var center = h / 2;
        var half = h / 2 * 0.92;
        var dx = w / (n - 1);

        double Y(int i, int sign) => center - sign * Math.Clamp(_current[i], 0f, 1f) * half;

        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(new Point(0, Y(0, 1)), true, true);
            for (var i = 1; i < n; i++) ctx.LineTo(new Point(i * dx, Y(i, 1)), true, true);
            for (var i = n - 1; i >= 0; i--) ctx.LineTo(new Point(i * dx, Y(i, -1)), true, true);
        }
        geo.Freeze();
        dc.DrawGeometry(_barBrush, null, geo);
    }
}
