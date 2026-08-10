using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.Windows.Input;

namespace Audiola.Avalonia.Controls;

public sealed class WaveformView : Control
{
    public static readonly StyledProperty<IReadOnlyList<float>?> PeaksProperty =
        AvaloniaProperty.Register<WaveformView, IReadOnlyList<float>?>(nameof(Peaks));

    public static readonly StyledProperty<IBrush> WaveBrushProperty =
        AvaloniaProperty.Register<WaveformView, IBrush>(nameof(WaveBrush), Brushes.DeepSkyBlue);

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<WaveformView, double>(nameof(Progress));

    public static readonly StyledProperty<ICommand?> SeekCommandProperty =
        AvaloniaProperty.Register<WaveformView, ICommand?>(nameof(SeekCommand));

    static WaveformView() => AffectsRender<WaveformView>(PeaksProperty, WaveBrushProperty, ProgressProperty);

    public WaveformView() => PointerPressed += OnPointerPressed;

    public IReadOnlyList<float>? Peaks
    {
        get => GetValue(PeaksProperty);
        set => SetValue(PeaksProperty, value);
    }

    public IBrush WaveBrush
    {
        get => GetValue(WaveBrushProperty);
        set => SetValue(WaveBrushProperty, value);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public ICommand? SeekCommand
    {
        get => GetValue(SeekCommandProperty);
        set => SetValue(SeekCommandProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var rect = Bounds;
        context.FillRectangle(new SolidColorBrush(Color.Parse("#1AFFFFFF")), rect);
        var peaks = Peaks;
        if (peaks is not { Count: > 1 } || rect.Width < 2 || rect.Height < 2) return;

        var pen = new Pen(WaveBrush, 1);
        var pairs = peaks.Count / 2;
        var center = rect.Center.Y;
        for (var x = 0; x < (int)rect.Width; x++)
        {
            var index = Math.Min(pairs - 1, x * pairs / Math.Max(1, (int)rect.Width));
            var min = Math.Clamp(peaks[index * 2], -1f, 1f);
            var max = Math.Clamp(peaks[index * 2 + 1], -1f, 1f);
            context.DrawLine(pen,
                new Point(rect.X + x + .5, center - max * rect.Height * .45),
                new Point(rect.X + x + .5, center - min * rect.Height * .45));
        }

        var playhead = rect.X + Math.Clamp(Progress, 0, 1) * rect.Width;
        context.DrawLine(new Pen(Brushes.White, 1.5), new Point(playhead, rect.Top), new Point(playhead, rect.Bottom));
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Bounds.Width <= 0 || SeekCommand is null) return;
        var ratio = Math.Clamp(e.GetPosition(this).X / Bounds.Width, 0, 1);
        if (SeekCommand.CanExecute(ratio))
            SeekCommand.Execute(ratio);
    }
}
