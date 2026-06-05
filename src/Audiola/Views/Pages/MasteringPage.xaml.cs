using System.Windows.Controls;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Pages;

public partial class MasteringPage : Page, INavigableView<MasteringViewModel>, INavigationAware
{
    public MasteringViewModel ViewModel { get; }

    public MasteringPage(MasteringViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public void OnNavigatedTo() => _ = ViewModel.PrepareFromStudioAsync();

    public void OnNavigatedFrom() => ViewModel.OnDeactivated();

    private void Waveform_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Controls.WaveformControl wf && wf.ActualWidth > 0)
            ViewModel.Transport.Seek(e.GetPosition(wf).X / wf.ActualWidth);
    }
}
