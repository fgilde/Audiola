using Audiola.Avalonia.Platform;
using Audiola.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace Audiola.Avalonia.Views.Pages;

public partial class EditorPage : UserControl, INavigationAware
{
    private readonly EditorViewModel _viewModel;
    private bool _dragging;
    private double _dragStartRatio;

    public EditorPage(EditorViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public void OnNavigatedTo()
    {
        _viewModel.EnsureLoaded();
        _viewModel.OnActivatedFx();
    }

    public void OnNavigatedFrom() => _viewModel.OnDeactivatedFx();

    private void Waveform_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Controls.WaveformControl wf || wf.Bounds.Width <= 0) return;
        _dragging = true;
        _dragStartRatio = e.GetPosition(wf).X / wf.Bounds.Width;
        e.Pointer.Capture(wf);
    }

    private void Waveform_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging || sender is not Controls.WaveformControl wf || wf.Bounds.Width <= 0) return;
        _viewModel.SetSelection(_dragStartRatio, e.GetPosition(wf).X / wf.Bounds.Width);
    }

    private void Waveform_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Controls.WaveformControl wf) return;
        e.Pointer.Capture(null);
        if (!_dragging) return;
        _dragging = false;

        var ratio = wf.Bounds.Width > 0 ? e.GetPosition(wf).X / wf.Bounds.Width : 0;

        // Kaum bewegt → als Klick werten: Auswahl löschen und an die Stelle springen.
        if (Math.Abs(ratio - _dragStartRatio) * wf.Bounds.Width < 4)
        {
            _viewModel.ClearSelectionCommand.Execute(null);
            _viewModel.Transport.Seek(ratio);
        }
    }
}
