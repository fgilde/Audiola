using Audiola.ViewModels;
using Avalonia.Controls;

namespace Audiola.Avalonia.Views.Pages;

public partial class ProvenancePage : UserControl
{
    public ProvenancePage(ProvenanceViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
