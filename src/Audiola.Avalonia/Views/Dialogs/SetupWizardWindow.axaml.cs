using Audiola.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Audiola.Avalonia.Views.Dialogs;

/// <summary>Geführter Ersteinrichtungs-Assistent (lokale Modelle, CUDA, Stem-Trennung, ElevenLabs).</summary>
public partial class SetupWizardWindow : Window
{
    /// <summary>Parameterloser Konstruktor nur für den XAML-Vorschau-Compiler.</summary>
    public SetupWizardWindow() => InitializeComponent();

    public SetupWizardWindow(SetupWizardViewModel viewModel)
    {
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
        InitializeComponent();
        Opened += async (_, _) => await viewModel.InitializeAsync();
        Closed += (_, _) => viewModel.RequestClose -= OnRequestClose;
    }

    private void OnRequestClose()
    {
        try { Close(); } catch { /* bereits geschlossen */ }
    }

    // „Später": Assistent schließen, ohne ihn als abgeschlossen zu markieren (startet beim nächsten Mal erneut).
    private void Later_Click(object? sender, RoutedEventArgs e) => Close();
}
