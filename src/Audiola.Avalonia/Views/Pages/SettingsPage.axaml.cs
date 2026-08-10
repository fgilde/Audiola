using Audiola.ViewModels;
using Avalonia.Controls;

namespace Audiola.Avalonia.Views.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
