using System.IO;
using System.Windows;
using Audiola.Models;
using Audiola.Views.Dialogs;
using NAudio.Wave;
using Wpf.Ui;

namespace Audiola.Services;

/// <summary>
/// Einheitlicher Export-Weg für die gesamte App: zeigt den <see cref="ExportDialog"/> (Format,
/// Bitrate, Tags, Cover, Lyrics), rendert dann die Audiodaten, schreibt die Datei (WAV/MP3/M4A/FLAC)
/// und bettet die Metadaten ein. Ersetzt die verstreuten SaveFileDialog-Aufrufe.
/// </summary>
public sealed class ExportService
{
    private readonly IMetadataService _meta;
    private readonly ISnackbarService _snackbar;
    private readonly FilePreviewHost _preview;

    public ExportService(IMetadataService meta, ISnackbarService snackbar, FilePreviewHost preview)
    {
        _meta = meta;
        _snackbar = snackbar;
        _preview = preview;
    }

    /// <summary>Bequeme Überladung für Stereo-Renderer, die interleaved Samples + Samplerate liefern.</summary>
    public Task ExportAsync(
        string defaultFileName,
        Func<Task<(float[] Samples, int SampleRate)>> renderStereo,
        AudioMetadata seed,
        string? seedLyrics = null,
        Func<bool, Task<string?>>? generateLyrics = null,
        bool elevenLabsAvailable = false)
        => ExportAsync(defaultFileName,
            async () => { var (s, sr) = await renderStereo(); return (ISampleProvider)new FloatArraySampleProvider(s, sr, 2); },
            seed, seedLyrics, generateLyrics, elevenLabsAvailable);

    /// <param name="defaultFileName">Vorschlag für den Dateinamen (ohne Endung).</param>
    /// <param name="render">Liefert den zu exportierenden Sample-Stream. Wird erst nach Bestätigung aufgerufen; ein <see cref="IDisposable"/>-Provider wird danach freigegeben.</param>
    /// <param name="seed">Vorbelegung der Tag-Felder (z. B. die projektweiten Song-Metadaten).</param>
    /// <param name="seedLyrics">Optionaler Liedtext (z. B. der Spur), falls die Tags keinen haben.</param>
    /// <param name="generateLyrics">Optionaler Callback zum Erzeugen von Lyrics im Dialog (z. B. Whisper auf der Quelle).</param>
    public async Task ExportAsync(
        string defaultFileName,
        Func<Task<ISampleProvider>> render,
        AudioMetadata seed,
        string? seedLyrics = null,
        Func<bool, Task<string?>>? generateLyrics = null,
        bool elevenLabsAvailable = false)
    {
        // Vorschau: in eine Temp-Datei rendern + taggen und in einem eingebetteten WebView2-Dialog
        // (mudex-Preview) anzeigen.
        async Task PreviewAsync(ExportRequest pr)
        {
            var p = await render();
            try { await Task.Run(() => AudioExporter.Export(p, pr.Path, pr.Bitrate)); }
            finally { (p as IDisposable)?.Dispose(); }
            try { await Task.Run(() => _meta.Write(pr.Path, pr.Metadata, pr.EmbedLyrics)); }
            catch { /* Tags sind für die Vorschau nicht kritisch */ }

            var url = _preview.GetPreviewUrl(pr.Path);
            var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                        ?? Application.Current?.MainWindow;
            new PreviewDialog(url, Path.GetFileName(pr.Path)) { Owner = owner }.ShowDialog();
        }

        var dialog = new ExportDialog(defaultFileName, seed, seedLyrics, generateLyrics, elevenLabsAvailable, PreviewAsync)
        {
            Owner = Application.Current?.MainWindow
        };
        if (dialog.ShowDialog() != true || dialog.Result is not { } req) return;

        try
        {
            var provider = await render();
            try
            {
                await Task.Run(() => AudioExporter.Export(provider, req.Path, req.Bitrate));
            }
            finally { (provider as IDisposable)?.Dispose(); }

            string? tagWarning = null;
            try { await Task.Run(() => _meta.Write(req.Path, req.Metadata, req.EmbedLyrics)); }
            catch (Exception tagEx) { tagWarning = tagEx.Message; }

            if (tagWarning is null)
                _snackbar.Success("Exportiert", Path.GetFileName(req.Path));
            else
                _snackbar.Warning("Exportiert (Tags eingeschränkt)",
                    $"{Path.GetFileName(req.Path)} — Metadaten konnten nicht vollständig geschrieben werden.", 5);
        }
        catch (Exception ex)
        {
            UiError.Show("Export fehlgeschlagen", ex.Message);
        }
    }
}
