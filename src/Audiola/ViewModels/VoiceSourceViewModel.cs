using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Audiola.ViewModels;

/// <summary>
/// Wiederverwendbare Stimmen-Quelle: vorhandene ElevenLabs-Stimme wählen, eigene Stimme
/// aufnehmen oder eine Audiodatei hochladen. Aufnahme/Upload werden zu einer Stimme geklont;
/// per <see cref="KeepVoice"/> bleibt sie im Konto, sonst wird sie nach Gebrauch gelöscht.
/// </summary>
public sealed partial class VoiceSourceViewModel : ObservableObject
{
    private readonly IVoiceChangeService _voice;
    private readonly IAudioRecorder _recorder;

    public VoiceSourceViewModel(IVoiceChangeService voice, IAudioRecorder recorder)
    {
        _voice = voice;
        _recorder = recorder;
        VoicesView = CollectionViewSource.GetDefaultView(_voices);
        VoicesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(VoiceInfo.CategoryLabel)));
    }

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
    public async Task<(string VoiceId, bool Temporary)?> ResolveAsync()
    {
        IsResolving = true;
        try
        {
            switch (Mode)
            {
                case 0:
                    return SelectedVoice is null ? null : (SelectedVoice.Id, false);
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
                    return (id, !KeepVoice);
                default:
                    return null;
            }
        }
        catch (Exception ex) { Status = "Fehler: " + ex.Message; return null; }
        finally { IsResolving = false; }
    }
}
