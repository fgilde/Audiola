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

    // Positionsleiste: beim Drücken pausieren, beim Loslassen an die Stelle springen (Punch-in).
    private void Pos_ScrubStart(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => (DataContext as SingAlongViewModel)?.BeginScrub();

    private void Pos_ScrubEnd(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is SingAlongViewModel vm && sender is System.Windows.Controls.Slider s)
            vm.EndScrub(s.Value);
    }
}
