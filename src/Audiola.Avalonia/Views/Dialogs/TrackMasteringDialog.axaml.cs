using Audiola.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Audiola.Avalonia.Views.Dialogs;

/// <summary>
/// „Spur mastern": hostet dasselbe <see cref="Controls.MasteringPanel"/> wie die Mastering-Seite,
/// mit der gewählten Spur als Quelle (temporär solo für die hörbare Live-Vorschau). Beim Schließen
/// werden Solo/Mute-Zustände wiederhergestellt und der Live-Master deaktiviert.
/// </summary>
public partial class TrackMasteringDialog : Window
{
    /// <summary>Parameterloser Konstruktor nur für den XAML-Vorschau-Compiler.</summary>
    public TrackMasteringDialog() => InitializeComponent();

    public TrackMasteringDialog(MasteringViewModel viewModel, StemTrackViewModel track)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.CloseDialogRequested += OnCloseRequested;
        Opened += (_, _) => _ = viewModel.PrepareFromTrackAsync(track);
        Closed += (_, _) =>
        {
            viewModel.CloseDialogRequested -= OnCloseRequested;
            viewModel.EndTrackPreview();
        };
    }

    private void OnCloseRequested()
    {
        try { Close(); } catch { /* bereits zu */ }
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
