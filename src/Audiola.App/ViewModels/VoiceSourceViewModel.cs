using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Audiola.Models;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Audiola.ViewModels;

/// <summary>Kopfzeile einer Stimmen-Kategorie in <see cref="VoiceSourceViewModel.VoicesView"/>.</summary>
public sealed record VoiceGroupHeader(string Name);

/// <summary>Aufgelöste Stimmen-Auswahl: lokale Stimme oder ElevenLabs.</summary>
public sealed class VoiceChoice
{
    public string Engine { get; init; } = "elevenlabs"; // "elevenlabs" | "local"
    public string? ElevenVoiceId { get; init; }
    public bool TemporaryEleven { get; init; }
    public VoiceProfile? LocalProfile { get; init; }
    public bool IsLocal => Engine == "local";

    // Lokaler Stimmtausch (seed-vc): Qualität/Ausdruck.
    public int DiffusionSteps { get; init; } = 50;
    public bool AutoF0Adjust { get; init; }
}

/// <summary>
/// Wiederverwendbare Stimmen-Quelle: vorhandene ElevenLabs-Stimme wählen, eigene Stimme
/// aufnehmen oder eine Audiodatei hochladen. Aufnahme/Upload werden zu einer Stimme geklont;
/// per <see cref="KeepVoice"/> bleibt sie im Konto, sonst wird sie nach Gebrauch gelöscht.
/// </summary>
public sealed partial class VoiceSourceViewModel : ObservableObject
{
    private readonly IVoiceChangeService _voice;
    private readonly IAudioRecorder _recorder;
    private readonly IVoiceProfileStore _profiles;
    private readonly IFileDialogs _files;

    public VoiceSourceViewModel(IVoiceChangeService voice, IAudioRecorder recorder, IVoiceProfileStore profiles,
        IFileDialogs files)
    {
        _voice = voice;
        _recorder = recorder;
        _profiles = profiles;
        _files = files;
        foreach (var p in profiles.Profiles.Where(p => p.IsLocal)) LocalProfiles.Add(p);
    }

    /// <summary>0 = ElevenLabs, 1 = lokale Stimme.</summary>
    [ObservableProperty] private int _engineMode;

    public ObservableCollection<VoiceProfile> LocalProfiles { get; } = [];
    [ObservableProperty] private VoiceProfile? _selectedLocalProfile;

    // Lokaler Stimmtausch-Feintuning (seed-vc)
    [ObservableProperty] private double _diffusionSteps = 50;
    [ObservableProperty] private bool _autoF0Adjust;

    private readonly List<VoiceInfo> _voices = [];

    /// <summary>
    /// Nach Kategorie gruppierte Liste: Kopfzeilen (<see cref="VoiceGroupHeader"/>) stehen als
    /// eigene, nicht wählbare Einträge zwischen den Stimmen. Ersetzt die WPF-eigene
    /// <c>ICollectionView</c>-Gruppierung, die es in Avalonia nicht gibt.
    /// </summary>
    public ObservableCollection<object> VoicesView { get; } = [];

    private void RebuildVoicesView()
    {
        VoicesView.Clear();
        foreach (var group in _voices.GroupBy(v => v.CategoryLabel))
        {
            VoicesView.Add(new VoiceGroupHeader(group.Key));
            foreach (var voice in group) VoicesView.Add(voice);
        }
    }

    /// <summary>0 = Stimme wählen, 1 = aufnehmen, 2 = Audio-Datei.</summary>
    [ObservableProperty] private int _mode;

    [ObservableProperty] private VoiceInfo? _selectedVoice;
    [ObservableProperty] private bool _isLoadingVoices;
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private string? _recordedPath;
    [ObservableProperty] private string? _uploadPath;
    [ObservableProperty] private string _newVoiceName = "Meine Stimme";
    [ObservableProperty] private bool _keepVoice;
    [ObservableProperty] private bool _isResolving;

    public async Task LoadVoicesAsync()
    {
        if (!_voice.HasApiKey) { Status = "Kein ElevenLabs API-Key — in den Einstellungen hinterlegen."; return; }
        IsLoadingVoices = true;
        try
        {
            var list = await _voice.GetVoicesAsync();
            _voices.Clear();
            foreach (var v in list) _voices.Add(v);
            RebuildVoicesView();
            SelectedVoice = _voices.FirstOrDefault();
            Status = $"{_voices.Count} Stimmen geladen.";
        }
        catch (Exception ex) { Status = "Fehler: " + ex.Message; }
        finally { IsLoadingVoices = false; }
    }

    [RelayCommand]
    private void ToggleRecord()
    {
        if (IsRecording)
        {
            _ = StopRecordingAsync();
            return;
        }
        try
        {
            _recorder.Start(RecordDeviceNumber);
            IsRecording = true;
            RecordedPath = null;
            Status = "Aufnahme läuft … sprich ins Mikrofon.";
        }
        catch (Exception ex) { Status = "Mikrofon-Fehler: " + ex.Message; }
    }

    /// <summary>Mikrofon (NAudio-Gerätenummer) für die Aufnahme.</summary>
    [ObservableProperty] private int _recordDeviceNumber;

    private async Task StopRecordingAsync()
    {
        RecordedPath = await _recorder.StopAsync();
        IsRecording = false;
        Status = string.IsNullOrEmpty(RecordedPath) ? "Aufnahme fehlgeschlagen." : "Aufnahme bereit.";
    }

    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        var path = await _files.OpenFileAsync("Audiodatei als Stimm-Vorlage", FileFilter.Audio, FileFilter.Any);
        if (path is not null)
        {
            UploadPath = path;
            Status = "Datei gewählt: " + Path.GetFileName(UploadPath);
        }
    }

    /// <summary>
    /// Liefert die zu verwendende Voice-ID. Bei Aufnahme/Upload wird geklont; das zweite
    /// Tupel-Feld sagt, ob die Stimme danach wieder gelöscht werden soll (temporär).
    /// </summary>
    public async Task<VoiceChoice?> ResolveAsync()
    {
        IsResolving = true;
        try
        {
            // Lokale, bereits erstellte Stimme.
            if (EngineMode == 1)
            {
                if (SelectedLocalProfile is null) { Status = "Bitte eine lokale Stimme wählen (oder unter „Stimmen“ erstellen)."; return null; }
                return new VoiceChoice
                {
                    Engine = "local",
                    LocalProfile = SelectedLocalProfile,
                    DiffusionSteps = (int)Math.Round(DiffusionSteps),
                    AutoF0Adjust = AutoF0Adjust
                };
            }

            // ElevenLabs.
            switch (Mode)
            {
                case 0:
                    return SelectedVoice is null ? null
                        : new VoiceChoice { Engine = "elevenlabs", ElevenVoiceId = SelectedVoice.Id, TemporaryEleven = false };
                case 1:
                case 2:
                    var sample = Mode == 1 ? RecordedPath : UploadPath;
                    if (string.IsNullOrEmpty(sample) || !File.Exists(sample))
                    {
                        Status = Mode == 1 ? "Bitte zuerst aufnehmen." : "Bitte zuerst eine Datei wählen.";
                        return null;
                    }
                    Status = "Stimme wird erstellt (Klonen) …";
                    var id = await _voice.CreateVoiceFromSamplesAsync(NewVoiceName, [sample]);
                    if (string.IsNullOrEmpty(id)) { Status = "Klonen fehlgeschlagen."; return null; }
                    return new VoiceChoice { Engine = "elevenlabs", ElevenVoiceId = id, TemporaryEleven = !KeepVoice };
                default:
                    return null;
            }
        }
        catch (Exception ex) { Status = "Fehler: " + ex.Message; return null; }
        finally { IsResolving = false; }
    }
}
