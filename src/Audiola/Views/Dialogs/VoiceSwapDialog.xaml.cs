using System.Windows;
using Audiola.Services;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Dialogs;

public partial class VoiceSwapDialog : FluentWindow
{
    private readonly VoiceSourceViewModel _vm;

    public VoiceSwapDialog()
    {
        _vm = new VoiceSourceViewModel(App.GetService<IVoiceChangeService>(), App.GetService<IAudioRecorder>());
        DataContext = _vm;
        InitializeComponent();
        Loaded += async (_, _) => await _vm.LoadVoicesAsync();
    }

    /// <summary>Aufgelöste Zielstimme (VoiceId) + ob sie nach Gebrauch gelöscht werden soll.</summary>
    public (string VoiceId, bool Temporary)? Result { get; private set; }

    private async void Ok_Click(object sender, RoutedEventArgs e)
    {
        var r = await _vm.ResolveAsync();
        if (r is null) return; // Status im Picker erklärt warum
        Result = r;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
