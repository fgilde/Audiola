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
        _theme = settings.Current.Theme == "Light" ? "Light" : "Dark";
    }

    partial void OnPythonPathChanged(string value) { _settings.Current.PythonPath = value; _settings.Save(); }
    partial void OnDemucsModelChanged(string value) { _settings.Current.DemucsModel = value; _settings.Save(); }
    partial void OnOutputDirectoryChanged(string value) { _settings.Current.OutputDirectory = value; _settings.Save(); }
    partial void OnElevenLabsApiKeyChanged(string value) { _settings.Current.ElevenLabsApiKey = value?.Trim() ?? ""; _settings.Save(); }
    partial void OnIsRecordingChanged(bool value) => OnPropertyChanged(nameof(RecordButtonText));

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
    /// <summary>Mikrofon (NAudio-Gerätenummer) für „Eigene Stimme aufnehmen".</summary>
    [ObservableProperty] private int _recordDeviceNumber;

    [RelayCommand]
    private async Task ToggleRecordAsync()
    {
        if (!IsRecording)
        {
            try
            {
                _recorder.Start(RecordDeviceNumber);
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
            await _voiceChange.CreateVoiceFromSamplesAsync(name, paths);
            VoiceStatus = $"Stimme '{name}' erstellt.";
            _snackbar.Success("Stimme erstellt", $"'{name}' steht jetzt im Stimmtausch-Dialog bereit.", 4);
        }
        catch (Exception ex)
        {
            VoiceStatus = "";
            ShowError("Stimme erstellen fehlgeschlagen", ex.Message);
        }
        finally { IsVoiceBusy = false; }
    }

    private void ShowError(string title, string message) =>
        _snackbar.Error(title, message, 6);

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

    /// <summary>Aktuelles Theme ("Light"/"Dark") — steuert die Markierung der Theme-Buttons.</summary>
    [ObservableProperty] private string _theme;

    public bool IsLightTheme => Theme == "Light";
    public bool IsDarkTheme => Theme != "Light";

    partial void OnThemeChanged(string value)
    {
        ThemeManager.Apply(value);
        _settings.Current.Theme = value;
        _settings.Save();
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));
    }

    [RelayCommand]
    private void SetThemeLight() => Theme = "Light";

    [RelayCommand]
    private void SetThemeDark() => Theme = "Dark";
}
