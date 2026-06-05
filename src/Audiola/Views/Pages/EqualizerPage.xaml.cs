using System.Windows.Controls;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Pages;

public partial class EqualizerPage : Page, INavigableView<EqualizerViewModel>, INavigationAware
{
    public EqualizerViewModel ViewModel { get; }

    public EqualizerPage(EqualizerViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public void OnNavigatedTo() => _ = ViewModel.OnActivatedAsync();

    public void OnNavigatedFrom() => ViewModel.OnDeactivated();
}
