using System.Collections.ObjectModel;
using System.IO;
using Audiola.Models;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NAudio.Wave;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Audiola.ViewModels;

/// <summary>
/// „Stimmen": lokale Modelle verwalten (Download, Gerät CUDA/CPU) und lokale Stimmen-Profile
/// erstellen/löschen — projektunabhängig im Stimmen-Store.
/// </summary>
public sealed partial class VoicesViewModel : ObservableObject
{
    private readonly ILocalVoiceService _local;
    private readonly IVoiceProfileStore _store;
    private readonly ISettingsService _settings;
    private readonly IAudioRecorder _recorder;
    private readonly ISnackbarService _snackbar;

    public VoicesViewModel(ILocalVoiceService local, IVoiceProfileStore store, ISettingsService settings,
        IAudioRecorder recorder, ISnackbarService snackbar)
    {
        _local = local;
        _store = store;
        _settings = settings;
        _recorder = recorder;
        _snackbar = snackbar;

        _selectedDevice = settings.Current.VoiceDevice;
        _whisperModel = settings.Current.WhisperModel;
        _store.Changed += (_, _) => RefreshProfiles();
        RefreshProfiles();
    }

    public bool ScriptAvailable => _local.ScriptAvailable;
    public IReadOnlyList<string> Devices { get; } = ["auto", "cuda", "cpu", "directml"];
    public IReadOnlyList<string> WhisperModels { get; } = ["tiny", "base", "small", "medium", "large-v3", "turbo"];

    public ObservableCollection<LocalVoiceModel> Models { get; } = [];
    public ObservableCollection<LocalVoiceModel> WhisperModelsList { get; } = [];
    public ObservableCollection<VoiceProfile> Profiles { get; } = [];

    [ObservableProperty] private string _selectedDevice;
    [ObservableProperty] private string _whisperModel;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "";

    // Vorschau
    [ObservableProperty] private string _previewText = "Hallo, das ist ein Test der lokalen Stimme.";
    private WaveOutEvent? _previewOut;
    private AudioFileReader? _previewReader;

    // Neues Profil
    [ObservableProperty] private string _newName = "Meine Stimme";
    [ObservableProperty] private LocalVoiceModel? _newModel;
    [ObservableProperty] private string _newLanguage = "de";
    [ObservableProperty] private string? _samplePath;
    [ObservableProperty] private bool _isRecording;

    partial void OnSelectedDeviceChanged(string value)
    {
        _settings.Current.VoiceDevice = value;
        _settings.Save();
    }

    partial void OnWhisperModelChanged(string value)
    {
        _settings.Current.WhisperModel = value;
        _settings.Save();
    }

    public async Task InitAsync() => await LoadModelsAsync();

    [RelayCommand]
    private async Task LoadModelsAsync()
    {
        IsBusy = true; Status = "Lade Modell-Liste …";
        try
        {
            var models = await _local.GetModelsAsync();
            Models.Clear();
            foreach (var m in models) Models.Add(m);
            NewModel ??= Models.FirstOrDefault(m => m.CanClone) ?? Models.FirstOrDefault();

            var whisper = await _local.GetWhisperModelsAsync();
            WhisperModelsList.Clear();
            foreach (var w in whisper) WhisperModelsList.Add(w);

            Status = ScriptAvailable ? $"{Models.Count} Modelle." : "Lokale Engine nicht installiert (voicebox_engine.py + Python-Pakete fehlen).";
        }
        catch (Exception ex) { Status = "Fehler: " + ex.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DownloadModel(LocalVoiceModel? model)
    {
        if (model is null) return;
        IsBusy = true; Status = $"Lade {model.Name} …";
        try
        {
            await _local.DownloadModelAsync(model.Id, new Progress<string>(s => Status = s));
            _snackbar.Show("Modell installiert", model.Name, ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(3));
            await LoadModelsAsync();
        }
        catch (Exception ex)
        {
            Status = "Fehler beim Download.";
            UiError.Show("Download fehlgeschlagen", ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void BrowseSample()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Stimm-Sample wählen",
            Filter = "Audiodateien|*.wav;*.mp3;*.flac;*.m4a;*.ogg|Alle Dateien|*.*"
        };
        if (dlg.ShowDialog() == true) SamplePath = dlg.FileName;
    }

    [RelayCommand]
    private void ToggleRecord()
    {
        if (IsRecording) { _ = StopRecordAsync(); return; }
        try { _recorder.Start(); IsRecording = true; Status = "Aufnahme läuft …"; }
        catch (Exception ex) { Status = "Mikrofon-Fehler: " + ex.Message; }
    }

    private async Task StopRecordAsync()
    {
        SamplePath = await _recorder.StopAsync();
        IsRecording = false;
        Status = string.IsNullOrEmpty(SamplePath) ? "Aufnahme fehlgeschlagen." : "Sample aufgenommen.";
    }

    [RelayCommand]
    private void CreateProfile()
    {
        if (string.IsNullOrWhiteSpace(NewName)) { Status = "Bitte einen Namen angeben."; return; }
        if (NewModel is null) { Status = "Bitte ein Modell wählen."; return; }
        if (NewModel.CanClone && string.IsNullOrEmpty(SamplePath))
        {
            Status = "Dieses Modell klont aus einem Sample — bitte Aufnahme oder Datei wählen.";
            return;
        }

        var profile = new VoiceProfile
        {
            Name = NewName.Trim(),
            Engine = "local",
            ModelId = NewModel.Id,
            Language = string.IsNullOrWhiteSpace(NewLanguage) ? "de" : NewLanguage.Trim(),
            Description = $"{NewModel.Name}"
        };
        var samples = string.IsNullOrEmpty(SamplePath) ? Array.Empty<string>() : [SamplePath];
        _store.Add(profile, samples);

        SamplePath = null;
        NewName = "Meine Stimme";
        Status = $"Stimme „{profile.Name}“ erstellt.";
        _snackbar.Show("Stimme erstellt", profile.Name, ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(2));
    }

    [RelayCommand]
    private void DeleteProfile(VoiceProfile? p)
    {
        if (p is not null) _store.Remove(p.Id);
    }

    [RelayCommand]
    private async Task PreviewProfile(VoiceProfile? p)
    {
        if (p is null || string.IsNullOrWhiteSpace(PreviewText)) return;
        StopPreview();
        IsBusy = true; Status = $"Erzeuge Vorschau mit „{p.Name}“ …";
        try
        {
            var (samples, sr) = await _local.SpeakAsync(PreviewText, p, 1.0);
            var dir = Path.Combine(Path.GetTempPath(), "Audiola", "voice");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"preview_{Guid.NewGuid():N}.wav");
            AudioEdits.WriteWav(path, samples, sr);

            _previewReader = new AudioFileReader(path);
            _previewOut = new WaveOutEvent();
            _previewOut.Init(_previewReader);
            _previewOut.Play();
            Status = "Vorschau läuft.";
        }
        catch (Exception ex)
        {
            Status = "Vorschau fehlgeschlagen.";
            UiError.Show("Vorschau fehlgeschlagen", ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void StopPreview()
    {
        _previewOut?.Stop();
        _previewOut?.Dispose();
        _previewOut = null;
        _previewReader?.Dispose();
        _previewReader = null;
    }

    private void RefreshProfiles()
    {
        Profiles.Clear();
        foreach (var p in _store.Profiles.Where(p => p.IsLocal)) Profiles.Add(p);
    }
}
