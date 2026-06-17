using System.IO;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Audiola.ViewModels;

/// <summary>
/// Tag-Editor für die projektweiten Song-Metadaten (Titel, Interpret, Album, Cover, Lyrics …).
/// Bearbeitet direkt den geteilten <see cref="SongMetadata"/>-Zustand; kann Tags aus einer Datei
/// laden, Cover setzen und Lyrics aus dem Studio-Mix per Whisper erzeugen.
/// </summary>
public sealed partial class MetadataViewModel : ObservableObject
{
    private readonly IMetadataService _metaSvc;
    private readonly TimelineViewModel _timeline;
    private readonly ISnackbarService _snackbar;

    public SongMetadata Meta { get; }

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "Diese Tags werden beim Export geschrieben und im Projekt gespeichert.";

    public MetadataViewModel(SongMetadata meta, IMetadataService metaSvc, TimelineViewModel timeline, ISnackbarService snackbar)
    {
        Meta = meta;
        _metaSvc = metaSvc;
        _timeline = timeline;
        _snackbar = snackbar;

        _timeline.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TimelineViewModel.HasTracks))
                GenerateLyricsCommand.NotifyCanExecuteChanged();
        };
    }

    partial void OnIsBusyChanged(bool value) => GenerateLyricsCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void ImportFromFile()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Tags aus Audiodatei laden",
            Filter = "Audio|*.mp3;*.m4a;*.flac;*.wav;*.ogg;*.aac;*.wma;*.aiff|Alle Dateien|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        Meta.Apply(_metaSvc.Read(dlg.FileName));
        Status = $"Tags aus „{Path.GetFileName(dlg.FileName)}“ übernommen.";
    }

    [RelayCommand]
    private void ChooseCover()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Cover-Bild wählen",
            Filter = "Bilder|*.jpg;*.jpeg;*.png;*.webp;*.bmp|Alle Dateien|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            Meta.CoverArt = File.ReadAllBytes(dlg.FileName);
            Meta.CoverMimeType = Path.GetExtension(dlg.FileName).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                _ => "image/jpeg"
            };
        }
        catch (Exception ex) { UiError.Show("Cover konnte nicht geladen werden", ex.Message); }
    }

    [RelayCommand]
    private void RemoveCover() => Meta.CoverArt = null;

    [RelayCommand]
    private void Clear() => Meta.Clear();

    private bool CanGenerate => !IsBusy && _timeline.HasTracks;

    /// <summary>True, wenn ElevenLabs als Lyrics-Engine verfügbar ist (API-Key hinterlegt).</summary>
    public bool ElevenLabsAvailable => _timeline.ElevenLabsAvailable;

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateLyricsAsync(string? engine)
    {
        var useEleven = string.Equals(engine, "eleven", StringComparison.OrdinalIgnoreCase);
        IsBusy = true;
        Status = useEleven ? "Studio-Mix wird via ElevenLabs transkribiert …" : "Studio-Mix wird transkribiert (Whisper) …";
        try
        {
            var mix = await _timeline.RenderMixToTempFileAsync();
            if (mix is null) { Status = "Keine Spuren im Studio."; return; }

            var lrc = await _timeline.TranscribeFileToLrcAsync(mix,
                string.IsNullOrWhiteSpace(Meta.Title) ? null : Meta.Title, useEleven);
            if (string.IsNullOrWhiteSpace(lrc)) { Status = "Keine Sprache erkannt."; return; }

            Meta.Lyrics = lrc!;
            Status = useEleven ? "Liedtext erzeugt (ElevenLabs)." : "Liedtext erzeugt (Whisper).";
            _snackbar.Show("Liedtext erzeugt", "Aus dem Studio-Mix transkribiert.",
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(3));
        }
        catch (Exception ex) { UiError.Show("Liedtext erzeugen fehlgeschlagen", ex.Message); Status = ""; }
        finally { IsBusy = false; }
    }
}
