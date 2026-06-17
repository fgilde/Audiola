using System.IO;
using System.Windows;
using System.Windows.Controls;
using Audiola.Models;
using Audiola.Services;
using Audiola.ViewModels;
using Microsoft.Win32;
using Wpf.Ui.Controls;

namespace Audiola.Views.Dialogs;

public partial class ExportDialog : FluentWindow
{
    private readonly string _defaultFileName;
    private readonly Func<bool, Task<string?>>? _generateLyrics;
    private readonly Func<ExportRequest, Task>? _preview;

    public SongMetadata Meta { get; }

    /// <summary>Gesetzt, wenn der Nutzer „Exportieren" bestätigt hat.</summary>
    public ExportRequest? Result { get; private set; }

    /// <param name="defaultFileName">Vorschlag für den Dateinamen (ohne Endung).</param>
    /// <param name="seed">Vorbelegung der Tag-Felder (z. B. Projekt-Metadaten).</param>
    /// <param name="seedLyrics">Optionaler Liedtext (z. B. der Spur), falls die Tags keinen haben.</param>
    /// <param name="generateLyrics">Optionaler Callback zum Erzeugen von Lyrics (bool = ElevenLabs statt lokalem Whisper).</param>
    /// <param name="elevenLabsAvailable">Zeigt zusätzlich den „via ElevenLabs"-Button, wenn ein API-Key hinterlegt ist.</param>
    /// <param name="preview">Optionaler Callback für die Browser-Vorschau (erzeugt eine Temp-Datei mit Tags und öffnet sie).</param>
    public ExportDialog(string defaultFileName, AudioMetadata seed, string? seedLyrics,
        Func<bool, Task<string?>>? generateLyrics, bool elevenLabsAvailable = false, Func<ExportRequest, Task>? preview = null)
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
            GenerateLyricsButton.Visibility = Visibility.Visible;
            if (elevenLabsAvailable) GenerateLyricsElevenButton.Visibility = Visibility.Visible;
        }
        if (_preview is not null) PreviewButton.Visibility = Visibility.Visible;
        UpdateFormatUi();
    }

    private async void Preview_Click(object sender, RoutedEventArgs e)
    {
        if (_preview is null) return;
        var fmt = SelectedFormat;
        var dir = Path.Combine(Path.GetTempPath(), "Audiola", "preview");
        Directory.CreateDirectory(dir);
        var temp = Path.Combine(dir, $"{Sanitize(_defaultFileName)}_{Guid.NewGuid():N}.{fmt}");

        var req = new ExportRequest
        {
            Path = temp,
            Bitrate = SelectedBitrate,
            Metadata = Meta.ToMetadata(),
            EmbedLyrics = EmbedLyricsToggle.IsChecked == true
        };

        PreviewButton.IsEnabled = false;
        PreviewBusy.Visibility = Visibility.Visible;
        try { await _preview(req); }
        catch (Exception ex) { UiError.Show("Vorschau fehlgeschlagen", ex.Message); }
        finally { PreviewButton.IsEnabled = true; PreviewBusy.Visibility = Visibility.Collapsed; }
    }

    private string SelectedFormat => (FormatBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "wav";

    private int SelectedBitrate =>
        int.TryParse((BitrateBox.SelectedItem as ComboBoxItem)?.Tag as string, out var b) ? b : 256_000;

    private void Format_Changed(object sender, SelectionChangedEventArgs e) => UpdateFormatUi();

    private void UpdateFormatUi()
    {
        if (WavHint is null) return; // SelectionChanged feuert ggf. vor vollständigem Aufbau
        var fmt = SelectedFormat;
        WavHint.IsOpen = fmt == "wav";
        BitratePanel.Visibility = fmt is "mp3" or "m4a" ? Visibility.Visible : Visibility.Hidden;
    }

    private void ChooseCover_Click(object sender, RoutedEventArgs e)
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

    private void RemoveCover_Click(object sender, RoutedEventArgs e) => Meta.CoverArt = null;

    private async void GenerateLyrics_Click(object sender, RoutedEventArgs e)
    {
        if (_generateLyrics is null) return;
        var useEleven = (sender as FrameworkElement)?.Tag as string == "eleven";

        GenerateLyricsButton.IsEnabled = false;
        GenerateLyricsElevenButton.IsEnabled = false;
        LyricsBusy.Visibility = Visibility.Visible;
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
            LyricsBusy.Visibility = Visibility.Collapsed;
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var fmt = SelectedFormat;
        var filterIndex = fmt switch { "mp3" => 2, "m4a" => 3, "flac" => 4, _ => 1 };
        var dlg = new SaveFileDialog
        {
            Title = "Exportieren",
            Filter = AudioExporter.SaveFilter,
            FilterIndex = filterIndex,
            FileName = Sanitize(_defaultFileName) + "." + fmt
        };
        if (dlg.ShowDialog() != true) return;

        Result = new ExportRequest
        {
            Path = dlg.FileName,
            Bitrate = SelectedBitrate,
            Metadata = Meta.ToMetadata(),
            EmbedLyrics = EmbedLyricsToggle.IsChecked == true
        };
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static string Sanitize(string name)
    {
        name = Path.GetFileNameWithoutExtension(name);
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "audiola-export" : name;
    }
}
