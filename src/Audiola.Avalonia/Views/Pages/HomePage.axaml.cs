using Audiola.Avalonia.Platform;
using Audiola.ViewModels;
using Avalonia.Controls;

namespace Audiola.Avalonia.Views.Pages;

public partial class HomePage : UserControl, INavigationAware
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    /// <summary>Projektliste beim Anzeigen frisch einlesen (kann sich extern geändert haben).</summary>
    public void OnNavigatedTo() => _viewModel.RefreshRecentProjects();

    public void OnNavigatedFrom() { }
}
