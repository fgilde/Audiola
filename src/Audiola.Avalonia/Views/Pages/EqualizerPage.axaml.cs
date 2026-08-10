using Audiola.Avalonia.Platform;
using Audiola.ViewModels;
using Avalonia.Controls;

namespace Audiola.Avalonia.Views.Pages;

public partial class EqualizerPage : UserControl, INavigationAware
{
    private readonly EqualizerViewModel _viewModel;

    public EqualizerPage(EqualizerViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public void OnNavigatedTo() => _ = _viewModel.OnActivatedAsync();

    public void OnNavigatedFrom() => _viewModel.OnDeactivated();
}
