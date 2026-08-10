using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Singola.ViewModels;

namespace Singola.Avalonia.Controls;

public sealed class KaraokeStage : Control
{
    private const double PixelsPerSecond = 90;
    private const double MidiLow = 38;
    private const double MidiHigh = 82;
    private readonly List<(int Player, double Time, double Midi)> _samples = [];
    private MainViewModel? _viewModel;

    public void Attach(MainViewModel viewModel)
    {
        if (_viewModel is not null) _viewModel.PitchSampled -= OnPitchSampled;
        _viewModel = viewModel;
        _viewModel.PitchSampled += OnPitchSampled;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.IsSinging)) _samples.Clear();
            InvalidateVisual();
        };
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_viewModel is null || Bounds.Height < 1) return;

        context.DrawRectangle(new SolidColorBrush(Color.Parse("#17111F")), null, Bounds);
        var now = _viewModel.Engine.PositionSeconds;
        var anchor = Bounds.Width * .88;
        var origin = anchor - now * PixelsPerSecond;
        var noteHeight = Math.Max(7, Bounds.Height / (MidiHigh - MidiLow) * 1.7);
        var noteBrush = new SolidColorBrush(Color.FromArgb(0x38, 0xff, 0xff, 0xff));
        var notePen = new Pen(new SolidColorBrush(Color.FromArgb(0x66, 0xff, 0xff, 0xff)));

        foreach (var note in _viewModel.Melody)
        {
            var y = MidiToY(note.Midi);
            context.DrawRectangle(noteBrush, notePen,
                new Rect(origin + note.Start * PixelsPerSecond, y - noteHeight / 2,
                    Math.Max(6, (note.End - note.Start) * PixelsPerSecond), noteHeight), 4, 4);
        }

        foreach (var group in _samples.Where(sample => sample.Midi > 0).GroupBy(sample => sample.Player))
        {
            var color = Color.TryParse(_viewModel.Players[group.Key].ColorHex, out var parsed) ? parsed : Colors.DeepSkyBlue;
            var pen = new Pen(new SolidColorBrush(color), 4.5, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
            (double X, double Y)? last = null;
            foreach (var sample in group)
            {
                (double X, double Y) point = (origin + sample.Time * PixelsPerSecond, MidiToY(sample.Midi));
                if (last is { } previous && point.X - previous.X < 20)
                    context.DrawLine(pen, new Point(previous.X, previous.Y), new Point(point.X, point.Y));
                last = point;
            }
        }

        context.DrawLine(new Pen(Brushes.White, 2), new Point(anchor, 0), new Point(anchor, Bounds.Height));
    }

    private double MidiToY(double midi) =>
        Bounds.Height * (1 - (Math.Clamp(midi, MidiLow, MidiHigh) - MidiLow) / (MidiHigh - MidiLow));

    private void OnPitchSampled(int player, double time, double midi)
    {
        _samples.Add((player, time, midi));
        _samples.RemoveAll(sample => sample.Time < time - 30);
        InvalidateVisual();
    }
}
