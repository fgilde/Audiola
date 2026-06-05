using System.Collections.ObjectModel;
using System.IO;
using Audiola.Models;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Audiola.ViewModels;

/// <summary>
/// Interaktiver 4-Band-Master-EQ für den Studio-Mix. Die Live-Vorschau hängt im
/// Ausgang der Mix-Engine (sofort hörbar). „Exportieren“ rendert den aktiven
/// Studio-Mix und legt die EQ-Kurve drüber.
/// </summary>
public sealed partial class EqualizerViewModel : ObservableObject
{
    private static readonly string EqDir = Path.Combine(Path.GetTempPath(), "Audiola", "eq");

    private readonly SessionState _session;
    private readonly LiveEqProcessor _liveEq;
    private readonly ISnackbarService _snackbar;
    private readonly TimelineViewModel _timeline;
    private readonly StemMixerEngine _engine;
    private string? _sourcePath;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = "Quelle wird vorbereitet …";
    [ObservableProperty] private string _sourceLabel = "";
    [ObservableProperty] private int _sampleRate = 44100;
    [ObservableProperty] private bool _isLivePreview = true;

    public ObservableCollection<EqBand> Bands { get; } =
    [
        new EqBand { Type = EqBandType.LowShelf,  Frequency = 100,   GainDb = 0, ColorHex = "#FF6B6B" },
        new EqBand { Type = EqBandType.Peaking,   Frequency = 500,   GainDb = 0, ColorHex = "#FFB454" },
        new EqBand { Type = EqBandType.Peaking,   Frequency = 3000,  GainDb = 0, ColorHex = "#5B8CFF" },
        new EqBand { Type = EqBandType.HighShelf, Frequency = 10000, GainDb = 0, ColorHex = "#9B8CFF" },
    ];

    public EqualizerViewModel(SessionState session, LiveEqProcessor liveEq, ISnackbarService snackbar,
        TimelineViewModel timeline, StemMixerEngine engine)
    {
        _session = session;
        _liveEq = liveEq;
        _snackbar = snackbar;
        _timeline = timeline;
        _engine = engine;

        Directory.CreateDirectory(EqDir);
        _liveEq.SetBands(Bands);
        foreach (var b in Bands)
            b.PropertyChanged += (_, _) => _liveEq.MarkDirty();
    }

    partial void OnIsLivePreviewChanged(bool value)
    {
        _liveEq.SetBands(Bands);
        _liveEq.Enabled = value;
    }

    /// <summary>Seite angezeigt: Master-EQ aktivieren und Studio-Mix als Export-Quelle rendern.</summary>
    public async Task OnActivatedAsync()
    {
        _liveEq.SetBands(Bands);
        _liveEq.Enabled = IsLivePreview;

        if (_timeline.Tracks.Count > 0 && _timeline.DurationSeconds > 0.01)
        {
            IsBusy = true; ExportCommand.NotifyCanExecuteChanged();
            StatusText = "Rendere Studio-Mix …";
            SampleRate = _session.CurrentTrack?.SampleRate ?? 44100;
            var dur = TimeSpan.FromSeconds(_timeline.DurationSeconds);
            var tracks = _timeline.Tracks.ToList();
            try
            {
                _sourcePath = await Task.Run(() =>
                {
                    var (samples, sr) = _engine.RenderRange(tracks, TimeSpan.Zero, dur);
                    var temp = Path.Combine(EqDir, $"mix_{Guid.NewGuid():N}.wav");
                    AudioEdits.WriteWav(temp, samples, sr);
                    return temp;
                });
                SourceLabel = $"Studio-Mix ({tracks.Count} Spuren)";
            }
            catch (Exception ex) { _sourcePath = null; SourceLabel = "Render-Fehler: " + ex.Message; }
            IsBusy = false;
        }
        else if (_session.CurrentTrack is not null)
        {
            _sourcePath = _session.CurrentTrack.FilePath;
            SampleRate = _session.CurrentTrack.SampleRate;
            SourceLabel = "Datei: " + _session.CurrentTrack.FileName;
        }
        else { _sourcePath = null; SourceLabel = "Keine Quelle — im Studio Spuren laden."; }

        StatusText = _sourcePath is null ? SourceLabel
            : $"Quelle: {SourceLabel} — Kurve ziehen (live), dann exportieren.";
        ExportCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Seite verlassen: Master-EQ aus (damit das Studio sonst neutral bleibt).</summary>
    public void OnDeactivated() => _liveEq.Enabled = false;

    /// <summary>EQ-Bänder für die Projektdatei serialisieren.</summary>
    public List<ProjectEqBandDto> ExportBands() =>
        Bands.Select(b => new ProjectEqBandDto
        {
            Type = b.Type.ToString(),
            Frequency = b.Frequency,
            GainDb = b.GainDb,
            Q = b.Q,
            ColorHex = b.ColorHex
        }).ToList();

    /// <summary>EQ-Bänder aus einem Projekt übernehmen (Struktur bleibt, Werte werden gesetzt).</summary>
    public void ImportBands(IReadOnlyList<ProjectEqBandDto> bands)
    {
        if (bands is null || bands.Count == 0) return;
        for (var i = 0; i < Bands.Count && i < bands.Count; i++)
        {
            Bands[i].Frequency = bands[i].Frequency;
            Bands[i].GainDb = bands[i].GainDb;
            Bands[i].Q = bands[i].Q;
        }
        _liveEq.SetBands(Bands);
        _liveEq.MarkDirty();
    }

    [RelayCommand]
    private void Reset()
    {
        foreach (var b in Bands) b.GainDb = 0;
        StatusText = "EQ zurückgesetzt (flach).";
    }

    private bool CanExport => _sourcePath is not null && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        if (_sourcePath is null) return;
        var dialog = new SaveFileDialog
        {
            Title = "EQ-Mix exportieren",
            Filter = AudioExporter.SaveFilter,
            FileName = "studio-eq.wav"
        };
        if (dialog.ShowDialog() != true) return;

        var path = _sourcePath;
        var bands = Bands.ToList();
        IsBusy = true; ExportCommand.NotifyCanExecuteChanged();
        try
        {
            await Task.Run(() =>
            {
                var temp = Process(path, bands);
                using var reader = new NAudio.Wave.AudioFileReader(temp);
                AudioExporter.Export(reader, dialog.FileName);
            });
            _snackbar.Show("Exportiert", Path.GetFileName(dialog.FileName),
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            _snackbar.Show("Export fehlgeschlagen", ex.Message,
                ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(5));
        }
        finally
        {
            IsBusy = false; ExportCommand.NotifyCanExecuteChanged();
        }
    }

    private static string Process(string path, List<EqBand> bands)
    {
        var (samples, sr) = AudioProcessingHelper.ReadStereo(path);
        var chainL = bands.Select(b => b.CreateFilter(sr)).ToList();
        var chainR = bands.Select(b => b.CreateFilter(sr)).ToList();

        var frames = samples.Length / 2;
        for (var i = 0; i < frames; i++)
        {
            var l = samples[i * 2];
            var r = samples[i * 2 + 1];
            foreach (var f in chainL) l = f.Process(l);
            foreach (var f in chainR) r = f.Process(r);
            samples[i * 2] = Math.Clamp(l, -1f, 1f);
            samples[i * 2 + 1] = Math.Clamp(r, -1f, 1f);
        }

        var temp = Path.Combine(EqDir, $"eq_{Guid.NewGuid():N}.wav");
        AudioEdits.WriteWav(temp, samples, sr);
        return temp;
    }
}
