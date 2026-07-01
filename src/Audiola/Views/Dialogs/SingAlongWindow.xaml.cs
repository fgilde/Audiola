using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Dialogs;

/// <summary>Einsing-Studio: Backing + Mikrofon, Karaoke-Lyrics, Ton-Feedback, Übernahme als Spur.</summary>
public partial class SingAlongWindow : FluentWindow
{
    public SingAlongWindow(SingAlongViewModel viewModel)
    {
        DataContext = viewModel;
        Closed += (_, _) => viewModel.Dispose();   // Playback/Aufnahme sauber beenden
        InitializeComponent();
        viewModel.Initialize();
    }
}
