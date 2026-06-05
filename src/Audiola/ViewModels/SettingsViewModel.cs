using System.Collections.ObjectModel;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Audiola.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IStemSeparationService _separation;
    private readonly IVoiceChangeService _voiceChange;
    private readonly IAudioRecorder _recorder;
    private readonly ISnackbarService _snackbar;

    [ObservableProperty]
    private string _pythonPath;

    [ObservableProperty]
    private string _demucsModel;

    [ObservableProperty]
    private string _outputDirectory;

    [ObservableProperty]
    private string _elevenLabsApiKey;

    [ObservableProperty]
    private string _elevenLabsVoiceId;

    /// <summary>Im Konto verfügbare Stimmen (für die Auswahl).</summary>
    public ObservableCollection<VoiceInfo> Voices { get; } = [];

    [ObservableProperty]
    private VoiceInfo? _selectedVoice;

    [ObservableProperty]
    private string _newVoiceName = "";

    [ObservableProperty]
    private bool _isVoiceBusy;

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private string _voiceStatus = "";

    public string RecordButtonText => IsRecording ? "Aufnahme stoppen & Stimme erstellen" : "Eigene Stimme aufnehmen";

    [ObservableProperty]
    private string _demucsStatus = "Nicht geprueft.";

    [ObservableProperty]
    private bool _isCheckingDemucs;

    public IReadOnlyList<string> AvailableModels { get; } =
        ["htdemucs", "htdemucs_ft", "htdemucs_6s", "mdx_extra", "mdx_extra_q"];

    public SettingsViewModel(
        ISettingsService settings,
        IStemSeparationService separation,
        IVoiceChangeService voiceChange,
        IAudioRecorder recorder,
        ISnackbarService snackbar)
    {
        _settings = settings;
        _separation = separation;
        _voiceChange = voiceChange;
        _recorder = recorder;
        _snackbar = snackbar;

        _pythonPath = settings.Current.PythonPath;
        _demucsModel = settings.Current.DemucsModel;
        _outputDirectory = settings.Current.OutputDirectory;
        _elevenLabsApiKey = settings.Current.ElevenLabsApiKey;
        _elevenLabsVoiceId = settings.Current.ElevenLabsVoiceId;
    }

    partial void OnPythonPathChanged(string value) { _settings.Current.PythonPath = value; _settings.Save(); }
    partial void OnDemucsModelChanged(string value) { _settings.Current.DemucsModel = value; _settings.Save(); }
    partial void OnOutputDirectoryChanged(string value) { _settings.Current.OutputDirectory = value; _settings.Save(); }
    partial void OnElevenLabsApiKeyChanged(string value) { _settings.Current.ElevenLabsApiKey = value?.Trim() ?? ""; _settings.Save(); }
    partial void OnElevenLabsVoiceIdChanged(string value) { _settings.Current.ElevenLabsVoiceId = value?.Trim() ?? ""; _settings.Save(); }
    partial void OnIsRecordingChanged(bool value) => OnPropertyChanged(nameof(RecordButtonText));

    partial void OnSelectedVoiceChanged(VoiceInfo? value)
    {
        if (value is not null && value.Id != ElevenLabsVoiceId)
            ElevenLabsVoiceId = value.Id; // schreibt + speichert die Voice-ID
    }

    /// <summary>Lädt die Stimmen des Kontos und wählt die aktuell hinterlegte aus.</summary>
    [RelayCommand]
    private async Task RefreshVoicesAsync()
    {
        IsVoiceBusy = true;
        VoiceStatus = "Lade Stimmen …";
        try
        {
            var voices = await _voiceChange.GetVoicesAsync();
            Voices.Clear();
            foreach (var v in voices) Voices.Add(v);
            SelectedVoice = Voices.FirstOrDefault(v => v.Id == _settings.Current.ElevenLabsVoiceId);
            VoiceStatus = $"{Voices.Count} Stimmen geladen.";
        }
        catch (Exception ex)
        {
            VoiceStatus = "";
            ShowError("Stimmen laden fehlgeschlagen", ex.Message);
        }
        finally { IsVoiceBusy = false; }
    }

    /// <summary>Erstellt eine Stimme aus einer vorhandenen Audiodatei (Instant Voice Cloning).</summary>
    [RelayCommand]
    private async Task CreateVoiceFromFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Audio-Sample(s) für die Stimme wählen",
            Filter = "Audio|*.wav;*.mp3;*.m4a;*.flac;*.ogg|Alle Dateien|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog() != true) return;
        await CreateVoiceAsync(dialog.FileNames);
    }

    /// <summary>Aufnahme starten/stoppen; beim Stoppen wird daraus eine Stimme erstellt.</summary>
    [RelayCommand]
    private async Task ToggleRecordAsync()
    {
        if (!IsRecording)
        {
            try
            {
                _recorder.Start();
                IsRecording = true;
                VoiceStatus = "Aufnahme läuft … sprich einige Sätze und stoppe dann.";
            }
            catch (Exception ex)
            {
                ShowError("Aufnahme fehlgeschlagen", ex.Message);
            }
            return;
        }

        // Stoppen → fertige WAV → Stimme erstellen.
        VoiceStatus = "Aufnahme wird verarbeitet …";
        var path = await _recorder.StopAsync();
        IsRecording = false;
        if (!string.IsNullOrEmpty(path))
            await CreateVoiceAsync([path]);
    }

    private async Task CreateVoiceAsync(IReadOnlyList<string> paths)
    {
        IsVoiceBusy = true;
        VoiceStatus = "Stimme wird erstellt …";
        try
        {
            var name = string.IsNullOrWhiteSpace(NewVoiceName) ? "Audiola-Stimme" : NewVoiceName.Trim();
            var id = await _voiceChange.CreateVoiceFromSamplesAsync(name, paths);
            await RefreshVoicesAsync();
            SelectedVoice = Voices.FirstOrDefault(v => v.Id == id) ?? SelectedVoice;
            VoiceStatus = $"Stimme '{name}' erstellt und ausgewählt.";
            _snackbar.Show("Stimme erstellt", $"'{name}' steht jetzt zum Tauschen bereit.",
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(4));
        }
        catch (Exception ex)
        {
            VoiceStatus = "";
            ShowError("Stimme erstellen fehlgeschlagen", ex.Message);
        }
        finally { IsVoiceBusy = false; }
    }

    private void ShowError(string title, string message) =>
        _snackbar.Show(title, message, ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(6));

    [RelayCommand]
    private void BrowseOutput()
    {
        var dialog = new OpenFolderDialog { Title = "Ausgabeordner waehlen" };
        if (dialog.ShowDialog() == true)
            OutputDirectory = dialog.FolderName;
    }

    [RelayCommand]
    private async Task CheckDemucsAsync()
    {
        IsCheckingDemucs = true;
        DemucsStatus = "Pruefe …";
        try
        {
            var ok = await _separation.IsAvailableAsync();
            DemucsStatus = ok ? "Demucs gefunden ✓" : "Demucs nicht gefunden ✗";
            _snackbar.Show(ok ? "Demucs OK" : "Demucs fehlt",
                ok ? "Stem-Trennung ist einsatzbereit." : "Bitte 'pip install demucs' ausfuehren.",
                ok ? ControlAppearance.Success : ControlAppearance.Caution,
                new SymbolIcon(ok ? SymbolRegular.CheckmarkCircle24 : SymbolRegular.Warning24),
                TimeSpan.FromSeconds(4));
        }
        finally
        {
            IsCheckingDemucs = false;
        }
    }

    [RelayCommand]
    private void SetThemeLight() => ApplicationThemeManager.Apply(ApplicationTheme.Light);

    [RelayCommand]
    private void SetThemeDark() => ApplicationThemeManager.Apply(ApplicationTheme.Dark);
}
