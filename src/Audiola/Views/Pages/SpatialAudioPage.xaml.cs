using System.Windows.Controls;
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
}
