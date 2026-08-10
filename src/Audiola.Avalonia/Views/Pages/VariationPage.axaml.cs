using Audiola.ViewModels;
using Avalonia.Controls;

namespace Audiola.Avalonia.Views.Pages;

public partial class VariationPage : UserControl
{
    public VariationPage(VariationViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
