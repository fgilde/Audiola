using System.Windows.Controls;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Pages;

public partial class VariationPage : Page, INavigableView<VariationViewModel>
{
    public VariationViewModel ViewModel { get; }

    public VariationPage(VariationViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
