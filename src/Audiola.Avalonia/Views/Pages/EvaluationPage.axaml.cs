using Audiola.ViewModels;
using Avalonia.Controls;

namespace Audiola.Avalonia.Views.Pages;

public partial class EvaluationPage : UserControl
{
    public EvaluationPage(EvaluationViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
