using System.Windows.Controls;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Pages;

public partial class EvaluationPage : Page, INavigableView<EvaluationViewModel>
{
    public EvaluationViewModel ViewModel { get; }

    public EvaluationPage(EvaluationViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
