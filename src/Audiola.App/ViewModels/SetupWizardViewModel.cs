using System.Collections.ObjectModel;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Audiola.ViewModels;

/// <summary>Ein im Assistenten installierbares lokales Modell.</summary>
public sealed partial class WizardModelItem : ObservableObject
{
    public WizardModelItem(string id, string name, string description, bool required)
    {
        Id = id; Name = name; Description = description; Required = required;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public bool Required { get; }

    [ObservableProperty] private bool _installed;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private string _status = "";

    /// <summary>„Laden“-Button sichtbar, solange weder installiert noch gerade ladend.</summary>
    public bool CanDownload => !Installed && !IsDownloading;
    partial void OnInstalledChanged(bool value) => OnPropertyChanged(nameof(CanDownload));
    partial void OnIsDownloadingChanged(bool value) => OnPropertyChanged(nameof(CanDownload));
}

/// <summary>
/// Geführter Einrichtungs-Assistent: stellt die lokale KI bereit (Sprachmodell + Stem-Trennung),
/// prüft/installiert CUDA und lässt optional einen ElevenLabs-Key setzen. Läuft beim ersten Start
/// und manuell über Hilfe → „Einrichtungs-Assistent“.
/// </summary>
public sealed partial class SetupWizardViewModel : ObservableObject
{
    public const int StepCount = 6;

    private readonly ILocalVoiceService _voice;
    private readonly IAdvancedSeparationService _separation;
    private readonly ISettingsService _settings;

    public event Action? RequestClose;

    public SetupWizardViewModel(ILocalVoiceService voice, IAdvancedSeparationService separation, ISettingsService settings)
    {
        _voice = voice;
        _separation = separation;
        _settings = settings;
        _device = string.IsNullOrWhiteSpace(settings.Current.VoiceDevice) ? "auto" : settings.Current.VoiceDevice;
        _elevenLabsApiKey = settings.Current.ElevenLabsApiKey;

        Models =
        [
            new("qwen3-tts", "Qwen3-TTS 1.7B", "Mehrsprachige Sprachsynthese & Stimm-Erzeugung. Wird mindestens benötigt.", required: true),
            new("seed-vc", "seed-vc (Stimmtausch)", "Tauscht die Stimme einer Spur, behält Melodie/Betonung (auch Gesang).", required: false),
            new("whisper-base", "Whisper base (Transkription)", "Erzeugt Liedtexte/Untertitel (LRC) aus Audio.", required: false),
        ];
    }

    public ObservableCollection<WizardModelItem> Models { get; }

    [ObservableProperty] private int _stepIndex;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _busyText = "";

    // ---- Navigation ----
    public bool CanBack => StepIndex > 0 && !IsBusy;
    public bool CanNext => StepIndex < StepCount - 1 && !IsBusy;
    public bool IsLastStep => StepIndex == StepCount - 1;
    public string StepCaption => $"Schritt {StepIndex + 1} von {StepCount}";

    partial void OnStepIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CanBack));
        OnPropertyChanged(nameof(CanNext));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(StepCaption));
        BackCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanBack));
        OnPropertyChanged(nameof(CanNext));
        BackCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
        CheckGpuCommand.NotifyCanExecuteChanged();
        InstallCudaCommand.NotifyCanExecuteChanged();
        InstallStemCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanBack))]
    private void Back() => StepIndex--;

    [RelayCommand(CanExecute = nameof(CanNext))]
    private void Next() => StepIndex++;

    [RelayCommand]
    private void Finish()
    {
        _settings.Current.SetupCompleted = true;
        _settings.Save();
        RequestClose?.Invoke();
    }

    // ---- Schritt: GPU / CUDA ----
    [ObservableProperty] private string _device;
    [ObservableProperty] private string _gpuStatus = "Noch nicht geprüft — auf „GPU prüfen“ klicken.";
    [ObservableProperty] private bool _cudaActive;
    [ObservableProperty] private bool _cudaInstallable;

    public IReadOnlyList<string> Devices { get; } = ["auto", "cuda", "cpu", "directml"];

    partial void OnDeviceChanged(string value)
    {
        _settings.Current.VoiceDevice = value;
        _settings.Save();
    }

    [RelayCommand(CanExecute = nameof(NotBusy))]
    private async Task CheckGpuAsync()
    {
        IsBusy = true; BusyText = "Prüfe GPU/CUDA …";
        try
        {
            var s = await _voice.CheckGpuAsync();
            GpuStatus = s.Summary;
            CudaActive = s.CudaAvailable;
            CudaInstallable = !s.CudaAvailable; // kein CUDA aktiv → Installation anbieten
        }
        catch (Exception ex) { GpuStatus = "Fehler: " + ex.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(NotBusy))]
    private async Task InstallCudaAsync()
    {
        IsBusy = true; BusyText = "Installiere CUDA-Torch … (groß, dauert einige Minuten)";
        try
        {
            await _voice.InstallCudaTorchAsync(new Progress<string>(m => BusyText = m));
            var s = await _voice.CheckGpuAsync();
            GpuStatus = s.Summary;
            CudaActive = s.CudaAvailable;
            CudaInstallable = !s.CudaAvailable;
        }
        catch (Exception ex) { UiError.Show("CUDA-Installation fehlgeschlagen", ex.Message); }
        finally { IsBusy = false; }
    }

    // ---- Schritt: Modelle ----
    public bool RequiredModelsInstalled => Models.Where(m => m.Required).All(m => m.Installed);

    [RelayCommand]
    private async Task DownloadModelAsync(WizardModelItem item)
    {
        if (item is null || item.IsDownloading || IsBusy) return;
        item.IsDownloading = true;
        item.Status = "Lädt …";
        try
        {
            await _voice.DownloadModelAsync(item.Id, new Progress<string>(m => item.Status = m));
            item.Installed = true;
            item.Status = "Installiert ✓";
            OnPropertyChanged(nameof(RequiredModelsInstalled));
        }
        catch (Exception ex)
        {
            item.Status = "";
            UiError.Show($"Download fehlgeschlagen: {item.Name}", ex.Message);
        }
        finally { item.IsDownloading = false; }
    }

    // ---- Schritt: Stem-Trennung ----
    [ObservableProperty] private bool _stemReady;
    [ObservableProperty] private string _stemStatus = "Noch nicht eingerichtet.";

    [RelayCommand(CanExecute = nameof(NotBusy))]
    private async Task InstallStemAsync()
    {
        IsBusy = true; BusyText = "Richte Stem-Trennung ein … (einmalig, groß)";
        try
        {
            await _separation.EnsureInstalledAsync(new Progress<string>(m => BusyText = m));
            StemReady = true;
            StemStatus = "Stem-Trennung bereit ✓";
        }
        catch (Exception ex)
        {
            StemStatus = "";
            UiError.Show("Stem-Trennung einrichten fehlgeschlagen", ex.Message);
        }
        finally { IsBusy = false; }
    }

    // ---- Schritt: ElevenLabs ----
    [ObservableProperty] private string _elevenLabsApiKey;

    partial void OnElevenLabsApiKeyChanged(string value)
    {
        _settings.Current.ElevenLabsApiKey = value?.Trim() ?? "";
        _settings.Save();
    }

    // ---- Abschluss-Status ----
    public bool LocalVoiceReady => Models.First(m => m.Id == "qwen3-tts").Installed;

    /// <summary>Beim Öffnen den aktuellen Stand ermitteln (Modelle installiert? GPU?).</summary>
    public async Task InitializeAsync()
    {
        try
        {
            var models = await _voice.GetModelsAsync();
            foreach (var item in Models)
            {
                var match = models.FirstOrDefault(m => string.Equals(m.Id, item.Id, StringComparison.OrdinalIgnoreCase));
                if (match is { Installed: true }) { item.Installed = true; item.Status = "Installiert ✓"; }
            }
            OnPropertyChanged(nameof(RequiredModelsInstalled));
            OnPropertyChanged(nameof(LocalVoiceReady));
        }
        catch { /* Liste optional */ }
        // GPU/torch wird NICHT automatisch geprüft (torch-Laden ist langsam) — der Nutzer klickt „GPU prüfen“.
    }

    private bool NotBusy => !IsBusy;
}
