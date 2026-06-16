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
        _vm = new VoiceSourceViewModel(App.GetService<IVoiceChangeService>(), App.GetService<IAudioRecorder>(),
            App.GetService<IVoiceProfileStore>());
        DataContext = _vm;
        InitializeComponent();
        Loaded += async (_, _) => await _vm.LoadVoicesAsync();
    }

    /// <summary>Aufgelöste Zielstimme (lokal oder ElevenLabs).</summary>
    public VoiceChoice? Result { get; private set; }

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
