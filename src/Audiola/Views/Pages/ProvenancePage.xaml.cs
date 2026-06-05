using System.Windows.Controls;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Pages;

public partial class ProvenancePage : Page, INavigableView<ProvenanceViewModel>
{
    public ProvenanceViewModel ViewModel { get; }

    public ProvenancePage(ProvenanceViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
