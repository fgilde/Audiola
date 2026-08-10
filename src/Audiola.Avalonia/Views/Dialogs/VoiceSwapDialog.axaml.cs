using Audiola.Services;
using Audiola.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Audiola.Avalonia.Views.Dialogs;

public partial class VoiceSwapDialog : Window
{
    private readonly VoiceSourceViewModel _vm;

    public VoiceSwapDialog()
    {
        _vm = new VoiceSourceViewModel(AppServices.Get<IVoiceChangeService>(), AppServices.Get<IAudioRecorder>(),
            AppServices.Get<IVoiceProfileStore>(), AppServices.Get<IFileDialogs>());
        DataContext = _vm;
        InitializeComponent();
        Opened += async (_, _) => await _vm.LoadVoicesAsync();
    }

    /// <summary>Aufgelöste Zielstimme (lokal oder ElevenLabs); null = abgebrochen.</summary>
    public VoiceChoice? Result { get; private set; }

    private async void Ok_Click(object? sender, RoutedEventArgs e)
    {
        if (await _vm.ResolveAsync() is not { } choice) return;
        Result = choice;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}
