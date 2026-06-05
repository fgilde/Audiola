using System.Windows.Controls;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Pages;

public partial class HomePage : Page, INavigableView<HomeViewModel>, INavigationAware
{
    public HomeViewModel ViewModel { get; }

    public HomePage(HomeViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public void OnNavigatedTo() => ViewModel.RefreshRecentProjects();

    public void OnNavigatedFrom() { }
}
