using System.Windows;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Dialogs;

/// <summary>Dialog zum Mastern einer einzelnen Studio-Spur (EQ → Kompressor → LUFS).</summary>
public partial class TrackMasteringDialog : FluentWindow
{
    public TrackMasteringDialog(TrackMasteringViewModel viewModel, StemTrackViewModel track)
    {
        DataContext = viewModel;
        viewModel.SetTrack(track);
        viewModel.RequestClose += () => { try { Close(); } catch { /* bereits zu */ } };
        Closed += (_, _) => viewModel.StopPreview();   // Audio-Ressourcen freigeben
        InitializeComponent();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
