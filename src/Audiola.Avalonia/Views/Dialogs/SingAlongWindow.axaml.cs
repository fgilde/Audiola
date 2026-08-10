using Audiola.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace Audiola.Avalonia.Views.Dialogs;

/// <summary>Einsing-Studio: Backing + Mikrofon, Karaoke-Lyrics, Ton-Feedback, Übernahme als Spur.</summary>
public partial class SingAlongWindow : Window
{
    /// <summary>Parameterloser Konstruktor nur für den XAML-Vorschau-Compiler.</summary>
    public SingAlongWindow() => InitializeComponent();

    public SingAlongWindow(SingAlongViewModel viewModel)
    {
        DataContext = viewModel;
        Closed += (_, _) => viewModel.Dispose();   // Playback/Aufnahme sauber beenden
        InitializeComponent();
        viewModel.Initialize();
    }

    // Positionsleiste: beim Drücken pausieren, beim Loslassen an die Stelle springen (Punch-in).
    private void Pos_ScrubStart(object? sender, PointerPressedEventArgs e)
        => (DataContext as SingAlongViewModel)?.BeginScrub();

    private void Pos_ScrubEnd(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is SingAlongViewModel vm && sender is Slider slider)
            vm.EndScrub(slider.Value);
    }
}
