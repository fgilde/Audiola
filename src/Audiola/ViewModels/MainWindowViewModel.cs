using CommunityToolkit.Mvvm.ComponentModel;

namespace Audiola.ViewModels;

/// <summary>Zustand des Hauptfensters (die frühere NavigationView-Menüliste ist der Werkzeug-Rail gewichen).</summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = "Audiola";

    /// <summary>Text der Statusleiste — zeigt laufende Hintergrund-Arbeit (Stems, Stimmtausch …).</summary>
    [ObservableProperty]
    private string _status = "Bereit";

    /// <summary>True, solange eine Hintergrund-Aufgabe läuft (Statuskugel wird gelb).</summary>
    [ObservableProperty]
    private bool _isWorking;
}
