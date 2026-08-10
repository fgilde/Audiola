using Audiola.Avalonia.Platform;
using Audiola.ViewModels;
using Avalonia.Controls;

namespace Audiola.Avalonia.Views.Pages;

public partial class MasteringPage : UserControl, INavigationAware
{
    private readonly MasteringViewModel _viewModel;

    public MasteringPage(MasteringViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public void OnNavigatedTo() => _ = _viewModel.PrepareFromStudioAsync();

    public void OnNavigatedFrom() => _viewModel.OnDeactivated();
}
