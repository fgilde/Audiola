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
}
