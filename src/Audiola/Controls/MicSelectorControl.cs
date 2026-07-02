using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NAudio.Wave;

namespace Audiola.Controls;

/// <summary>
/// Mikrofon-Auswahl mit Live-Pegelanzeige: ComboBox aller Aufnahme-Geräte plus ein kleiner
/// Aussteuerungsbalken, der sofort zeigt, ob (und welches) Mikrofon Signal liefert.
/// Bindet über <see cref="DeviceNumber"/> (TwoWay) an das ViewModel; die Geräteliste verwaltet
/// das Control selbst. Das Monitoring läuft nur, solange das Control geladen ist (WASAPI shared —
/// stört parallele Aufnahmen nicht).
/// </summary>
public sealed class MicSelectorControl : Control
{
    public static readonly DependencyProperty DeviceNumberProperty = DependencyProperty.Register(
        nameof(DeviceNumber), typeof(int), typeof(MicSelectorControl),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, _) => ((MicSelectorControl)d).RestartMonitor()));

    /// <summary>NAudio-Gerätenummer (WaveInEvent.DeviceNumber) des gewählten Mikrofons.</summary>
    public int DeviceNumber
    {
        get => (int)GetValue(DeviceNumberProperty);
        set => SetValue(DeviceNumberProperty, value);
    }

    private readonly ComboBox _combo = new() { MinWidth = 220, VerticalAlignment = VerticalAlignment.Center };
    private readonly Border _track = new()
    {
        Width = 74, Height = 8, CornerRadius = new CornerRadius(4), Margin = new Thickness(8, 0, 0, 0),
        VerticalAlignment = VerticalAlignment.Center,
        Background = new SolidColorBrush(Color.FromArgb(0x35, 0x7F, 0x7F, 0x7F)),
        ClipToBounds = true,
    };
    private readonly Border _fill;
    private WaveInEvent? _monitor;
    private float _level;      // geglättet 0..1
    private bool _suppress;    // Reentranz bei Listen-Refresh

    public MicSelectorControl()
    {
        _fill = new Border
        {
            Height = 8, CornerRadius = new CornerRadius(4), Width = 0,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = MakeGradient(),
        };
        _track.Child = _fill;

        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(_combo);
        panel.Children.Add(_track);
        AddVisualChild(panel);
        _panel = panel;

        _combo.ToolTip = "Aufnahme-Gerät wählen — der Balken zeigt den Live-Pegel.";
        _combo.SelectionChanged += (_, _) =>
        {
            if (!_suppress && _combo.SelectedItem is MicItem m) DeviceNumber = m.Index;
        };
        _combo.DropDownOpened += (_, _) => RefreshDevices();

        Loaded += (_, _) => { RefreshDevices(); RestartMonitor(); };
        Unloaded += (_, _) => StopMonitor();
    }

    private readonly StackPanel _panel;
    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index) => _panel;
    protected override Size MeasureOverride(Size constraint)
    {
        _panel.Measure(constraint);
        return _panel.DesiredSize;
    }
    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        _panel.Arrange(new Rect(arrangeBounds));
        return arrangeBounds;
    }

    private sealed record MicItem(int Index, string Name) { public override string ToString() => Name; }

    private void RefreshDevices()
    {
        _suppress = true;
        try
        {
            var current = DeviceNumber;
            _combo.Items.Clear();
            for (var i = 0; i < WaveInEvent.DeviceCount; i++)
                _combo.Items.Add(new MicItem(i, WaveInEvent.GetCapabilities(i).ProductName));
            _combo.SelectedItem = _combo.Items.OfType<MicItem>().FirstOrDefault(m => m.Index == current)
                                  ?? _combo.Items.OfType<MicItem>().FirstOrDefault();
            if (_combo.SelectedItem is MicItem sel && sel.Index != current) DeviceNumber = sel.Index;
        }
        finally { _suppress = false; }
    }

    private void RestartMonitor()
    {
        StopMonitor();
        if (!IsLoaded || WaveInEvent.DeviceCount == 0) return;
        try
        {
            var m = new WaveInEvent
            {
                DeviceNumber = Math.Clamp(DeviceNumber, 0, WaveInEvent.DeviceCount - 1),
                WaveFormat = new WaveFormat(22050, 16, 1),
                BufferMilliseconds = 40,
            };
            m.DataAvailable += OnMonitorData;
            m.StartRecording();
            _monitor = m;
        }
        catch { _monitor = null; SetLevel(0); /* Gerät belegt/entfernt → kein Pegel */ }
    }

    private void StopMonitor()
    {
        if (_monitor is null) return;
        try { _monitor.DataAvailable -= OnMonitorData; _monitor.StopRecording(); _monitor.Dispose(); } catch { }
        _monitor = null;
        SetLevel(0);
    }

    private void OnMonitorData(object? sender, WaveInEventArgs e)
    {
        float peak = 0;
        for (var i = 0; i + 1 < e.BytesRecorded; i += 2)
        {
            var s = Math.Abs((int)BitConverter.ToInt16(e.Buffer, i)) / 32768f;
            if (s > peak) peak = s;
        }
        _level = peak > _level ? peak : _level * 0.78f;   // schnell rauf, weich runter
        var lvl = _level;
        Dispatcher.BeginInvoke(() => SetLevel(lvl));
    }

    private void SetLevel(float v) => _fill.Width = Math.Clamp(v, 0, 1) * _track.Width;

    private static Brush MakeGradient()
    {
        var g = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0x3D, 0xDC, 0x84), 0.0));
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xC2, 0x4B), 0.75));
        g.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x53, 0x50), 1.0));
        g.Freeze();
        return g;
    }
}
