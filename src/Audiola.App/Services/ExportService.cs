using System.IO;
using Audiola.Models;
using NAudio.Wave;

namespace Audiola.Services;

/// <summary>
/// Einheitlicher Export-Weg für die gesamte App: zeigt den Export-Dialog des Hosts (Format,
/// Bitrate, Tags, Cover, Lyrics), rendert dann die Audiodaten, schreibt die Datei (WAV/MP3/M4A/FLAC)
/// und bettet die Metadaten ein. Ersetzt die verstreuten SaveFileDialog-Aufrufe.
/// </summary>
public sealed class ExportService
{
    private readonly IMetadataService _meta;
    private readonly INotifier _snackbar;
    private readonly FilePreviewHost _preview;
    private readonly IAppDialogs _dialogs;

    public ExportService(IMetadataService meta, INotifier snackbar, FilePreviewHost preview, IAppDialogs dialogs)
    {
        _meta = meta;
        _snackbar = snackbar;
        _preview = preview;
        _dialogs = dialogs;
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
        // Vorschau: in eine Temp-Datei rendern + taggen und in der Vorschau des Hosts
        // (mudex-Preview) anzeigen.
        async Task PreviewAsync(ExportRequest pr)
        {
            var p = await render();
            try { await Task.Run(() => AudioExporter.Export(p, pr.Path, pr.Bitrate)); }
            finally { (p as IDisposable)?.Dispose(); }
            try { await Task.Run(() => _meta.Write(pr.Path, pr.Metadata, pr.EmbedLyrics)); }
            catch { /* Tags sind für die Vorschau nicht kritisch */ }

            await _dialogs.ShowFilePreviewAsync(_preview.GetPreviewUrl(pr.Path), Path.GetFileName(pr.Path));
        }

        var req = await _dialogs.ShowExportAsync(new ExportDialogRequest(
            defaultFileName, seed, seedLyrics, generateLyrics, elevenLabsAvailable, PreviewAsync));
        if (req is null) return;

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
