using Audiola.Avalonia.ViewModels;
using Avalonia.Controls;

namespace Audiola.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    public MainWindow(AudiolaHostViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
