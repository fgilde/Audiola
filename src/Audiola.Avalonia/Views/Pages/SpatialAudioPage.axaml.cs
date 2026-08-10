using Audiola.Avalonia.Platform;
using Audiola.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace Audiola.Avalonia.Views.Pages;

public partial class SpatialAudioPage : UserControl, INavigationAware
{
    private readonly SpatialAudioViewModel _viewModel;
    private SpatialSourceViewModel? _dragDot;

    public SpatialAudioPage(SpatialAudioViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public void OnNavigatedTo() => _viewModel.PrepareFromStudio();

    public void OnNavigatedFrom() => _viewModel.OnDeactivated();

    // ---- Punkte im Radar direkt ziehen (aktualisiert Azimut/Distanz → Regler) ----

    private void Dot_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: SpatialSourceViewModel vm } control)
        {
            _dragDot = vm;
            e.Pointer.Capture(control);
            UpdateDotFromPointer(e);
            e.Handled = true;
        }
    }

    private void Dot_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragDot is not null && sender is Control control && ReferenceEquals(e.Pointer.Captured, control))
            UpdateDotFromPointer(e);
    }

    private void Dot_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        e.Pointer.Capture(null);
        _dragDot = null;
    }

    private void UpdateDotFromPointer(PointerEventArgs e)
    {
        if (_dragDot is null) return;
        var p = e.GetPosition(RadarArea);          // 0..220, Mitte = (110,110)
        double dx = p.X - 110, dy = p.Y - 110;
        var r = Math.Sqrt(dx * dx + dy * dy);
        _dragDot.Distance = Math.Round(Math.Clamp(r / 45.0, 0.2, 2.0), 2);
        // x = sin(az)*r, y = -cos(az)*r  →  az = atan2(dx, -dy)
        var azDeg = Math.Atan2(dx, -dy) * 180.0 / Math.PI;
        _dragDot.AzimuthDeg = Math.Round(azDeg);
    }
}
