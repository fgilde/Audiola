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
        if (n == 0 || w <= 0 || h <= 0) return;

        const double gap = 2;
        var barW = Math.Max(1.0, (w - gap * (n - 1)) / n);
        for (var i = 0; i < n; i++)
        {
            var lv = Math.Clamp(_current[i], 0f, 1f);
            var bh = lv * h;
            if (bh < 0.8) continue;
            var x = i * (barW + gap);
            dc.DrawRoundedRectangle(_barBrush, null, new Rect(x, h - bh, barW, bh), 1.2, 1.2);
        }
    }
}
