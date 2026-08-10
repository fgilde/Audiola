using System.IO;
using Audiola.Models;
using Audiola.Services;
using Audiola.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Audiola.Avalonia.Views.Dialogs;

public partial class ExportDialog : Window
{
    private readonly string _defaultFileName;
    private readonly Func<bool, Task<string?>>? _generateLyrics;
    private readonly Func<ExportRequest, Task>? _preview;

    public SongMetadata Meta { get; }

    /// <summary>Gesetzt, wenn der Nutzer „Exportieren" bestätigt hat.</summary>
    public ExportRequest? Result { get; private set; }

    /// <summary>Parameterloser Konstruktor nur für den XAML-Vorschau-Compiler.</summary>
    public ExportDialog() : this("audiola-export", new AudioMetadata(), null, null) { }

    public ExportDialog(string defaultFileName, AudioMetadata seed, string? seedLyrics,
        Func<bool, Task<string?>>? generateLyrics, bool elevenLabsAvailable = false,
        Func<ExportRequest, Task>? preview = null)
    {
        _defaultFileName = string.IsNullOrWhiteSpace(defaultFileName) ? "audiola-export" : defaultFileName;
        _generateLyrics = generateLyrics;
        _preview = preview;

        Meta = new SongMetadata();
        Meta.Apply(seed);
        if (!string.IsNullOrWhiteSpace(seedLyrics) && string.IsNullOrWhiteSpace(Meta.Lyrics))
            Meta.Lyrics = seedLyrics!;

        DataContext = this;
        InitializeComponent();

        if (_generateLyrics is not null)
        {
            GenerateLyricsButton.IsVisible = true;
            if (elevenLabsAvailable) GenerateLyricsElevenButton.IsVisible = true;
        }
        if (_preview is not null) PreviewButton.IsVisible = true;
        UpdateFormatUi();
    }

    private string SelectedFormat => (FormatBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "wav";

    private int SelectedBitrate =>
        int.TryParse((BitrateBox.SelectedItem as ComboBoxItem)?.Tag as string, out var b) ? b : 256_000;

    private void Format_Changed(object? sender, SelectionChangedEventArgs e) => UpdateFormatUi();

    private void UpdateFormatUi()
    {
        if (WavHint is null) return; // SelectionChanged feuert ggf. vor vollständigem Aufbau
        var fmt = SelectedFormat;
        WavHint.IsVisible = fmt == "wav";
        BitratePanel.IsVisible = fmt is "mp3" or "m4a";
    }

    private async void Preview_Click(object? sender, RoutedEventArgs e)
    {
        if (_preview is null) return;
        var temp = TempDir.File("preview", $".{SelectedFormat}", Sanitize(_defaultFileName));

        var request = new ExportRequest
        {
            Path = temp,
            Bitrate = SelectedBitrate,
            Metadata = Meta.ToMetadata(),
            EmbedLyrics = EmbedLyricsToggle.IsChecked == true
        };

        PreviewButton.IsEnabled = false;
        PreviewBusy.IsVisible = true;
        try { await _preview(request); }
        catch (Exception ex) { UiError.Show("Vorschau fehlgeschlagen", ex.Message); }
        finally { PreviewButton.IsEnabled = true; PreviewBusy.IsVisible = false; }
    }

    private async void ChooseCover_Click(object? sender, RoutedEventArgs e)
    {
        var path = await AppServices.Get<IFileDialogs>()
            .OpenFileAsync("Cover-Bild wählen", FileFilter.Image, FileFilter.Any);
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

    private void RemoveCover_Click(object? sender, RoutedEventArgs e) => Meta.CoverArt = null;

    private async void GenerateLyrics_Click(object? sender, RoutedEventArgs e)
    {
        if (_generateLyrics is null) return;
        var useEleven = (sender as Control)?.Tag as string == "eleven";

        GenerateLyricsButton.IsEnabled = false;
        GenerateLyricsElevenButton.IsEnabled = false;
        LyricsBusy.IsVisible = true;
        try
        {
            var lrc = await _generateLyrics(useEleven);
            if (!string.IsNullOrWhiteSpace(lrc)) Meta.Lyrics = lrc!;
        }
        catch (Exception ex) { UiError.Show("Liedtext erzeugen fehlgeschlagen", ex.Message); }
        finally
        {
            GenerateLyricsButton.IsEnabled = true;
            GenerateLyricsElevenButton.IsEnabled = true;
            LyricsBusy.IsVisible = false;
        }
    }

    private async void Export_Click(object? sender, RoutedEventArgs e)
    {
        var fmt = SelectedFormat;
        // Das gewählte Format zuerst anbieten, die übrigen als Alternativen.
        FileFilter[] filters =
        [
            FormatFilter(fmt),
            .. new[] { "wav", "mp3", "m4a", "flac" }.Where(f => f != fmt).Select(FormatFilter),
            FileFilter.Any
        ];

        var path = await AppServices.Get<IFileDialogs>()
            .SaveFileAsync("Exportieren", Sanitize(_defaultFileName) + "." + fmt, filters);
        if (path is null) return;

        // Endung sicherstellen — Linux-Dialoge hängen sie nicht automatisch an.
        if (!Path.GetExtension(path).TrimStart('.').Equals(fmt, StringComparison.OrdinalIgnoreCase))
            path = Path.ChangeExtension(path, fmt);

        Result = new ExportRequest
        {
            Path = path,
            Bitrate = SelectedBitrate,
            Metadata = Meta.ToMetadata(),
            EmbedLyrics = EmbedLyricsToggle.IsChecked == true
        };
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();

    private static FileFilter FormatFilter(string format) => format switch
    {
        "mp3" => new FileFilter("MP3", "mp3"),
        "m4a" => new FileFilter("M4A / AAC", "m4a"),
        "flac" => new FileFilter("FLAC", "flac"),
        _ => new FileFilter("WAV", "wav")
    };

    private static string Sanitize(string name)
    {
        name = Path.GetFileNameWithoutExtension(name);
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "audiola-export" : name;
    }
}
