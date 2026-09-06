using System.Windows;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Dialogs;

/// <summary>Geführter Ersteinrichtungs-Assistent (lokale Modelle, CUDA, Stem-Trennung, ElevenLabs).</summary>
public partial class SetupWizardWindow : FluentWindow
{
    public SetupWizardWindow(SetupWizardViewModel viewModel)
    {
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
        InitializeComponent();
        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }

    private void OnRequestClose()
    {
        try { Close(); } catch { /* bereits geschlossen */ }
    }
}
