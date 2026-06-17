using System.Windows;
using Audiola.Services;
using Audiola.ViewModels;
using Wpf.Ui.Controls;

namespace Audiola.Views.Dialogs;

public partial class TextToSpeechDialog : FluentWindow
{
    private readonly VoiceSourceViewModel _vm;

    public TextToSpeechDialog()
    {
        _vm = new VoiceSourceViewModel(App.GetService<IVoiceChangeService>(), App.GetService<IAudioRecorder>(),
            App.GetService<IVoiceProfileStore>());
        DataContext = _vm;
        InitializeComponent();
        Loaded += async (_, _) => await _vm.LoadVoicesAsync();
    }

    public VoiceChoice? Result { get; private set; }
    public string Text => TtsTextInput.Text;
    public double Speed => SpeedSlider.Value;
    public double Stability => StabilitySlider.Value;
    public double Similarity => SimilaritySlider.Value;

    private async void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TtsTextInput.Text)) return;
        var r = await _vm.ResolveAsync();
        if (r is null) return;
        Result = r;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
