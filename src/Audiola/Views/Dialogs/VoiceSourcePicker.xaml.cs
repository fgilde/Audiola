using System.Windows;
using System.Windows.Controls;
using Audiola.ViewModels;

namespace Audiola.Views.Dialogs;

public partial class VoiceSourcePicker : UserControl
{
    public VoiceSourcePicker() => InitializeComponent();

    private void OnLoaded(object sender, RoutedEventArgs e) => SyncMode();

    private void Mode_Checked(object sender, RoutedEventArgs e) => SyncMode();

    private void SyncMode()
    {
        // Wird ggf. schon während des Parsens ausgelöst, bevor alle Panels existieren.
        if (PanelPick is null || PanelRec is null || PanelFile is null) return;

        var mode = ModeRec.IsChecked == true ? 1 : ModeFile.IsChecked == true ? 2 : 0;
        PanelPick.Visibility = mode == 0 ? Visibility.Visible : Visibility.Collapsed;
        PanelRec.Visibility = mode == 1 ? Visibility.Visible : Visibility.Collapsed;
        PanelFile.Visibility = mode == 2 ? Visibility.Visible : Visibility.Collapsed;

        if (DataContext is VoiceSourceViewModel vm) vm.Mode = mode;
    }
}
