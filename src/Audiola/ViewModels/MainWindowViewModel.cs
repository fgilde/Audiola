using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Controls;

namespace Audiola.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = "Audiola";

    [ObservableProperty]
    private ObservableCollection<object> _menuItems =
    [
        new NavigationViewItem("Start", SymbolRegular.Home24, typeof(Views.Pages.HomePage)),
        new NavigationViewItem("Studio", SymbolRegular.Timeline24, typeof(Views.Pages.TimelinePage)),
        new NavigationViewItem("Equalizer", SymbolRegular.Options24, typeof(Views.Pages.EqualizerPage)),
        new NavigationViewItem("Mastering", SymbolRegular.SoundWaveCircle24, typeof(Views.Pages.MasteringPage)),
        new NavigationViewItem("Klangvariation", SymbolRegular.Pulse24, typeof(Views.Pages.VariationPage)),
        new NavigationViewItem("Provenienz", SymbolRegular.DocumentSearch24, typeof(Views.Pages.ProvenancePage)),
        new NavigationViewItem("Mess-Harness", SymbolRegular.DataHistogram24, typeof(Views.Pages.EvaluationPage)),
    ];

    [ObservableProperty]
    private ObservableCollection<object> _footerMenuItems =
    [
        new NavigationViewItem("Einstellungen", SymbolRegular.Settings24, typeof(Views.Pages.SettingsPage)),
    ];
}
