using System.IO;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    private readonly INotifier _snackbar;
    private readonly IFileDialogs _files;

    public SongMetadata Meta { get; }

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "Diese Tags werden beim Export geschrieben und im Projekt gespeichert.";

    public MetadataViewModel(SongMetadata meta, IMetadataService metaSvc, TimelineViewModel timeline,
        INotifier snackbar, IFileDialogs files)
    {
        Meta = meta;
        _metaSvc = metaSvc;
        _timeline = timeline;
        _snackbar = snackbar;
        _files = files;

        _timeline.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TimelineViewModel.HasTracks))
                GenerateLyricsCommand.NotifyCanExecuteChanged();
        };
    }

    partial void OnIsBusyChanged(bool value) => GenerateLyricsCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private async Task ImportFromFileAsync()
    {
        var path = await _files.OpenFileAsync("Tags aus Audiodatei laden", FileFilter.Audio, FileFilter.Any);
        if (path is null) return;
        Meta.Apply(_metaSvc.Read(path));
        Status = $"Tags aus „{Path.GetFileName(path)}“ übernommen.";
    }

    [RelayCommand]
    private async Task ChooseCoverAsync()
    {
        var path = await _files.OpenFileAsync("Cover-Bild wählen", FileFilter.Image, FileFilter.Any);
        if (path is null) return;
        try
        {
            Meta.CoverArt = File.ReadAllBytes(path);
            Meta.CoverMimeType = Path.GetExtension(path).ToLowerInvariant() switch
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
            _snackbar.Success("Liedtext erzeugt", "Aus dem Studio-Mix transkribiert.");
        }
        catch (Exception ex) { UiError.Show("Liedtext erzeugen fehlgeschlagen", ex.Message); Status = ""; }
        finally { IsBusy = false; }
    }
}
