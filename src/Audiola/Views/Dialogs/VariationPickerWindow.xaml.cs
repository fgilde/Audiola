using System.Windows;
using Audiola.Services;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Dialogs;

public partial class VariationPickerWindow : FluentWindow
{
    private readonly VariationPickerViewModel _vm;

    public VariationPickerWindow(IReadOnlyList<IAudioVariationProvider> providers, string scopeLabel)
    {
        _vm = new VariationPickerViewModel(providers, scopeLabel);
        DataContext = _vm;
        InitializeComponent();
    }

    public IAudioVariationProvider? SelectedProvider => _vm.SelectedProvider;
    public IReadOnlyList<string> SelectedVariationIds => _vm.SelectedVariationIds;

    private void Apply_Click(object sender, RoutedEventArgs e) { DialogResult = true; Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
