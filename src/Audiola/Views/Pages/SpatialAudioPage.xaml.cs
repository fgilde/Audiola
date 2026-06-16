using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Pages;

public partial class SpatialAudioPage : Page, INavigableView<SpatialAudioViewModel>, INavigationAware
{
    public SpatialAudioViewModel ViewModel { get; }

    public SpatialAudioPage(SpatialAudioViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public void OnNavigatedTo() => ViewModel.PrepareFromStudio();

    public void OnNavigatedFrom() => ViewModel.OnDeactivated();

    // ---- Punkte im Radar direkt ziehen (aktualisiert Azimut/Distanz → Regler) ----
    private SpatialSourceViewModel? _dragDot;

    private void Dot_Down(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SpatialSourceViewModel vm } fe)
        {
            _dragDot = vm;
            fe.CaptureMouse();
            UpdateDotFromMouse(e);
            e.Handled = true;
        }
    }

    private void Dot_Move(object sender, MouseEventArgs e)
    {
        if (_dragDot is not null && sender is FrameworkElement { IsMouseCaptured: true })
            UpdateDotFromMouse(e);
    }

    private void Dot_Up(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe) fe.ReleaseMouseCapture();
        _dragDot = null;
    }

    private void UpdateDotFromMouse(MouseEventArgs e)
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
