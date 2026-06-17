using System.Windows.Controls;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Pages;

public partial class MetadataPage : Page, INavigableView<MetadataViewModel>
{
    public MetadataViewModel ViewModel { get; }

    public MetadataPage(MetadataViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
