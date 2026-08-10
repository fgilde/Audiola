using Audiola.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Audiola.Avalonia.Views.Dialogs;

/// <summary>
/// Wiederverwendbare Stimmen-Quelle (ElevenLabs-Stimme wählen, aufnehmen, Datei hochladen
/// oder lokale Stimme). Die Radio-Knöpfe setzen die Zustände im ViewModel; die Panels
/// blenden sich per Bindung selbst ein.
/// </summary>
public partial class VoiceSourcePicker : UserControl
{
    public VoiceSourcePicker() => InitializeComponent();

    private VoiceSourceViewModel? Vm => DataContext as VoiceSourceViewModel;

    private void Engine_Cloud(object? sender, RoutedEventArgs e) { if (Vm is { } vm) vm.EngineMode = 0; }

    private void Engine_Local(object? sender, RoutedEventArgs e) { if (Vm is { } vm) vm.EngineMode = 1; }

    private void Mode_Pick(object? sender, RoutedEventArgs e) { if (Vm is { } vm) vm.Mode = 0; }

    private void Mode_Record(object? sender, RoutedEventArgs e) { if (Vm is { } vm) vm.Mode = 1; }

    private void Mode_File(object? sender, RoutedEventArgs e) { if (Vm is { } vm) vm.Mode = 2; }
}
