using System.Windows;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Dialogs;

/// <summary>
/// „Spur mastern": hostet dasselbe <see cref="Controls.MasteringPanel"/> wie die Mastering-Seite,
/// mit der gewählten Spur als Quelle (temporär solo für die hörbare Live-Vorschau). Beim Schließen
/// werden Solo/Mute-Zustände wiederhergestellt und der Live-Master deaktiviert.
/// </summary>
public partial class TrackMasteringDialog : FluentWindow
{
    private readonly MasteringViewModel _viewModel;

    public TrackMasteringDialog(MasteringViewModel viewModel, StemTrackViewModel track)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        viewModel.CloseDialogRequested += OnCloseRequested;
        Loaded += (_, _) => _ = viewModel.PrepareFromTrackAsync(track);
        Closed += (_, _) =>
        {
            viewModel.CloseDialogRequested -= OnCloseRequested;
            viewModel.EndTrackPreview();
        };
    }

    private void OnCloseRequested() { try { Close(); } catch { /* bereits zu */ } }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
