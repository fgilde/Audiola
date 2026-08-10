using Audiola.Services;
using Audiola.Services.Audio;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.VisualTree;
using Avalonia.Layout;
using Avalonia.Media;

namespace Audiola.Controls;

/// <summary>
/// Mikrofon-Auswahl mit Live-Pegelanzeige: Liste aller Aufnahme-Geräte plus ein kleiner
/// Aussteuerungsbalken, der sofort zeigt, ob (und welches) Mikrofon Signal liefert.
/// Bindet über <see cref="DeviceNumber"/> (TwoWay) an das ViewModel.
///
/// Anders als die WPF-Fassung fragt dieses Control nicht NAudio direkt, sondern
/// <see cref="IAudioPlatform"/> — dadurch funktioniert es auch auf macOS und Linux.
/// </summary>
public sealed class MicSelectorControl : TemplatedControl
{
    public static readonly StyledProperty<int> DeviceNumberProperty =
        AvaloniaProperty.Register<MicSelectorControl, int>(nameof(DeviceNumber),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Index des gewählten Mikrofons in der Geräteliste der Plattform.</summary>
    public int DeviceNumber
    {
        get => GetValue(DeviceNumberProperty);
        set => SetValue(DeviceNumberProperty, value);
    }

    private readonly ComboBox _combo = new() { MinWidth = 220, VerticalAlignment = VerticalAlignment.Center };
    private readonly Border _track;
    private readonly Border _fill;
    private IAudioPlatform? _platform;
    private IAudioCapture? _monitor;
    private bool IsAttached => this.GetVisualRoot() is not null;
    private float _level;      // geglättet 0..1
    private bool _suppress;    // Reentranz bei Listen-Refresh

    public MicSelectorControl()
    {
        _fill = new Border
        {
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Width = 0,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = MakeGradient()
        };
        _track = new Border
        {
            Width = 74,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.FromArgb(0x35, 0x7F, 0x7F, 0x7F)),
            ClipToBounds = true,
            Child = _fill
        };

        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(_combo);
        panel.Children.Add(_track);
        Template = new FuncControlTemplate((_, _) => panel);

        ToolTip.SetTip(_combo, "Aufnahme-Gerät wählen — der Balken zeigt den Live-Pegel.");
        _combo.SelectionChanged += (_, _) =>
        {
            if (!_suppress && _combo.SelectedItem is MicItem m) DeviceNumber = m.Index;
        };
        _combo.DropDownOpened += (_, _) => RefreshDevices();

        AttachedToVisualTree += (_, _) => { RefreshDevices(); RestartMonitor(); };
        DetachedFromVisualTree += (_, _) => StopMonitor();
    }

    static MicSelectorControl() =>
        DeviceNumberProperty.Changed.AddClassHandler<MicSelectorControl, int>((c, _) => c.RestartMonitor());

    private sealed record MicItem(int Index, string Name)
    {
        public override string ToString() => Name;
    }

    /// <summary>Die Audio-Plattform kommt aus dem Container (das Control wird in XAML erzeugt).</summary>
    private IAudioPlatform? Platform
    {
        get
        {
            try { return _platform ??= AppServices.Get<IAudioPlatform>(); }
            catch { return null; }
        }
    }

    private void RefreshDevices()
    {
        var devices = SafeDevices();
        _suppress = true;
        try
        {
            var current = DeviceNumber;
            var items = devices.Select((d, i) => new MicItem(i, d.Name)).ToList();
            _combo.ItemsSource = items;
            _combo.SelectedItem = items.FirstOrDefault(m => m.Index == current) ?? items.FirstOrDefault();
            if (_combo.SelectedItem is MicItem sel && sel.Index != current) DeviceNumber = sel.Index;
        }
        finally { _suppress = false; }
    }

    private IReadOnlyList<AudioInputDevice> SafeDevices()
    {
        try { return Platform?.GetInputDevices() ?? []; }
        catch { return []; }
    }

    private void RestartMonitor()
    {
        StopMonitor();
        if (!IsAttached) return;

        var devices = SafeDevices();
        if (devices.Count == 0) return;

        try
        {
            var device = devices[Math.Clamp(DeviceNumber, 0, devices.Count - 1)];
            var capture = Platform!.CreateCapture(device.Id);
            capture.SamplesAvailable += OnMonitorData;
            capture.Start();
            _monitor = capture;
        }
        catch { _monitor = null; SetLevel(0); /* Gerät belegt/entfernt → kein Pegel */ }
    }

    private void StopMonitor()
    {
        if (_monitor is null) return;
        try { _monitor.SamplesAvailable -= OnMonitorData; _monitor.Stop(); _monitor.Dispose(); } catch { }
        _monitor = null;
        SetLevel(0);
    }

    private void OnMonitorData(object? sender, AudioSamplesEventArgs e)
    {
        float peak = 0;
        var span = e.Samples.Span;
        for (var i = 0; i < span.Length; i++)
        {
            var s = Math.Abs(span[i]);
            if (s > peak) peak = s;
        }
        _level = peak > _level ? peak : _level * 0.78f;   // schnell rauf, weich runter
        var level = _level;
        DispatcherHelper.PostToUi(() => SetLevel(level));
    }

    private void SetLevel(float v) => _fill.Width = Math.Clamp(v, 0, 1) * _track.Width;

    private static IBrush MakeGradient() => new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops =
        [
            new GradientStop(Color.FromRgb(0x3D, 0xDC, 0x84), 0.0),
            new GradientStop(Color.FromRgb(0xFF, 0xC2, 0x4B), 0.75),
            new GradientStop(Color.FromRgb(0xFF, 0x53, 0x50), 1.0)
        ]
    };
}
