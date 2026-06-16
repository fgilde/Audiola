using System.Windows.Controls;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Pages;

public partial class VoicesPage : Page, INavigableView<VoicesViewModel>, INavigationAware
{
    public VoicesViewModel ViewModel { get; }

    public VoicesPage(VoicesViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public void OnNavigatedTo() => _ = ViewModel.InitAsync();

    public void OnNavigatedFrom() { }
}
