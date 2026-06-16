using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using Audiola.Models;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Audiola.ViewModels;

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

    public VoiceSourceViewModel(IVoiceChangeService voice, IAudioRecorder recorder, IVoiceProfileStore profiles)
    {
        _voice = voice;
        _recorder = recorder;
        _profiles = profiles;
        VoicesView = CollectionViewSource.GetDefaultView(_voices);
        VoicesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(VoiceInfo.CategoryLabel)));
        foreach (var p in profiles.Profiles.Where(p => p.IsLocal)) LocalProfiles.Add(p);
    }

    /// <summary>0 = ElevenLabs, 1 = lokale Stimme.</summary>
    [ObservableProperty] private int _engineMode;

    public ObservableCollection<VoiceProfile> LocalProfiles { get; } = [];
    [ObservableProperty] private VoiceProfile? _selectedLocalProfile;

    // Lokaler Stimmtausch-Feintuning (seed-vc)
    [ObservableProperty] private double _diffusionSteps = 50;
    [ObservableProperty] private bool _autoF0Adjust;

    private readonly ObservableCollection<VoiceInfo> _voices = [];
    public ICollectionView VoicesView { get; }

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
            VoicesView.Refresh();
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
            _recorder.Start();
            IsRecording = true;
            RecordedPath = null;
            Status = "Aufnahme läuft … sprich ins Mikrofon.";
        }
        catch (Exception ex) { Status = "Mikrofon-Fehler: " + ex.Message; }
    }

    private async Task StopRecordingAsync()
    {
        RecordedPath = await _recorder.StopAsync();
        IsRecording = false;
        Status = string.IsNullOrEmpty(RecordedPath) ? "Aufnahme fehlgeschlagen." : "Aufnahme bereit.";
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Audiodatei als Stimm-Vorlage",
            Filter = "Audiodateien|*.wav;*.mp3;*.flac;*.m4a;*.ogg|Alle Dateien|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            UploadPath = dlg.FileName;
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
