using Audiola.ViewModels;
using Avalonia.Controls;

namespace Audiola.Avalonia.Views.Pages;

public partial class MetadataPage : UserControl
{
    public MetadataPage(MetadataViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
