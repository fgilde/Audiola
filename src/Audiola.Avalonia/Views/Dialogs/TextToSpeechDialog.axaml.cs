using Audiola.Services;
using Audiola.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Audiola.Avalonia.Views.Dialogs;

public partial class TextToSpeechDialog : Window
{
    private readonly VoiceSourceViewModel _vm;

    public TextToSpeechDialog()
    {
        _vm = new VoiceSourceViewModel(AppServices.Get<IVoiceChangeService>(), AppServices.Get<IAudioRecorder>(),
            AppServices.Get<IVoiceProfileStore>(), AppServices.Get<IFileDialogs>());
        DataContext = _vm;
        InitializeComponent();
        Opened += async (_, _) => await _vm.LoadVoicesAsync();
    }

    /// <summary>Gesetzt, wenn der Nutzer erzeugen wollte und eine Stimme auflösbar war.</summary>
    public TextToSpeechRequest? Result { get; private set; }

    private async void Ok_Click(object? sender, RoutedEventArgs e)
    {
        var text = TtsTextInput.Text;
        if (string.IsNullOrWhiteSpace(text)) return;
        if (await _vm.ResolveAsync() is not { } voice) return;
        Result = new TextToSpeechRequest(text, voice, SpeedSlider.Value, StabilitySlider.Value, SimilaritySlider.Value);
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}
