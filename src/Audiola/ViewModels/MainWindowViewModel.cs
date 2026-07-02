using CommunityToolkit.Mvvm.ComponentModel;

namespace Audiola.ViewModels;

/// <summary>Zustand des Hauptfensters (die frühere NavigationView-Menüliste ist der Werkzeug-Rail gewichen).</summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = "Audiola";
}
