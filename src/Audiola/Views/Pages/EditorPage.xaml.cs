using System.Windows.Controls;
using System.Windows.Input;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Pages;

public partial class EditorPage : Page, INavigableView<EditorViewModel>, INavigationAware
{
    private bool _dragging;
    private double _dragStartRatio;

    public EditorViewModel ViewModel { get; }

    public EditorPage(EditorViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public void OnNavigatedTo()
    {
        ViewModel.EnsureLoaded();
        ViewModel.OnActivatedFx();
    }

    public void OnNavigatedFrom() => ViewModel.OnDeactivatedFx();

    private void Waveform_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Controls.WaveformControl wf || wf.ActualWidth <= 0) return;
        _dragging = true;
        _dragStartRatio = e.GetPosition(wf).X / wf.ActualWidth;
        wf.CaptureMouse();
    }

    private void Waveform_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging || sender is not Controls.WaveformControl wf || wf.ActualWidth <= 0) return;
        var ratio = e.GetPosition(wf).X / wf.ActualWidth;
        ViewModel.SetSelection(_dragStartRatio, ratio);
    }

    private void Waveform_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Controls.WaveformControl wf) return;
        wf.ReleaseMouseCapture();
        if (!_dragging) return;
        _dragging = false;

        var ratio = wf.ActualWidth > 0 ? e.GetPosition(wf).X / wf.ActualWidth : 0;

        // Kaum bewegt → als Klick werten: Auswahl löschen und an die Stelle springen.
        if (System.Math.Abs(ratio - _dragStartRatio) * wf.ActualWidth < 4)
        {
            ViewModel.ClearSelectionCommand.Execute(null);
            ViewModel.Transport.Seek(ratio);
        }
    }
}
