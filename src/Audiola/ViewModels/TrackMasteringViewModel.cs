using System.Collections.ObjectModel;
using System.IO;
using Audiola.Models;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Audiola.ViewModels;

/// <summary>
/// Mastering einer einzelnen Studio-Spur (EQ → Kompressor → LUFS) — dieselbe Kette und dieselben
/// Profile (eingebaute &amp; eigene) wie die Mastering-Seite. Ergebnis ersetzt die Spur im Studio
/// (mit Undo) oder wird exportiert.
/// </summary>
public sealed partial class TrackMasteringViewModel : ObservableObject
{
    private readonly IMasteringService _mastering;
    private readonly TimelineViewModel _timeline;
    private readonly ExportService _export;
    private readonly SongMetadata _songMeta;
    private readonly ISettingsService _settings;
    private readonly ISnackbarService _snackbar;

    private readonly AbComparePlayer _preview = new();
    private string? _origCache;   // gerendertes, ungemastertes Original (einmalig, für die Vorschau)

    private StemTrackViewModel? _track;
    public event Action? RequestClose;

    public TrackMasteringViewModel(IMasteringService mastering, TimelineViewModel timeline,
        ExportService export, SongMetadata songMeta, ISettingsService settings, ISnackbarService snackbar)
    {
        _mastering = mastering;
        _timeline = timeline;
        _export = export;
        _songMeta = songMeta;
        _settings = settings;
        _snackbar = snackbar;
        RebuildProfileNames();
        _preview.PositionChanged += (_, _) => { UpdatePreviewTime(); IsPreviewPlaying = _preview.IsPlaying; };
        _preview.StateChanged += (_, _) => IsPreviewPlaying = _preview.IsPlaying;
    }

    public void SetTrack(StemTrackViewModel track)
    {
        _track = track;
        TrackName = track.Name;
    }

    [ObservableProperty] private string _trackName = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "Profil wählen oder Regler einstellen, dann anwenden.";

    // Profile (eingebaute + eigene).
    public ObservableCollection<string> ProfileNames { get; } = [];
    [ObservableProperty] private string? _selectedProfile;

    // Vorschau / A-B-Vergleich (Original vs. gemastert).
    [ObservableProperty] private bool _previewLoaded;
    [ObservableProperty] private bool _isPreviewPlaying;
    [ObservableProperty] private bool _showMastered = true;
    [ObservableProperty] private string _previewTime = "0:00 / 0:00";

    partial void OnShowMasteredChanged(bool value) => _preview.ShowB = value;

    // EQ
    [ObservableProperty] private bool _highPassEnabled = true;
    [ObservableProperty] private double _highPassHz = 30;
    [ObservableProperty] private double _bassGainDb;
    [ObservableProperty] private double _midGainDb;
    [ObservableProperty] private double _trebleGainDb;

    // Kompressor
    [ObservableProperty] private bool _compressorEnabled = true;
    [ObservableProperty] private double _thresholdDb = -18;
    [ObservableProperty] private double _ratio = 2.0;
    [ObservableProperty] private double _makeupGainDb;

    // Loudness
    [ObservableProperty] private bool _normalizeLoudness = true;
    [ObservableProperty] private double _targetLufs = -14;

    // Erweiterte Profil-Parameter (kein eigener Slider — kommen aus den Profilen).
    private double _lowShelfHz = 120, _midHz = 1000, _midQ = 1.0, _highShelfHz = 10000, _attackMs = 10, _releaseMs = 150;

    private void RebuildProfileNames()
    {
        ProfileNames.Clear();
        foreach (var p in MasteringProfiles.All) ProfileNames.Add(p.Name);
        foreach (var u in _settings.Current.UserMasteringProfiles) ProfileNames.Add(u.Name);
    }

    partial void OnSelectedProfileChanged(string? value)
    {
        var s = MasteringProfiles.All.FirstOrDefault(p => p.Name == value)?.Settings
                ?? _settings.Current.UserMasteringProfiles.FirstOrDefault(u => u.Name == value)?.Settings;
        if (s is null) return;

        HighPassEnabled = s.HighPassEnabled; HighPassHz = s.HighPassHz;
        BassGainDb = s.LowShelfGainDb; MidGainDb = s.MidGainDb; TrebleGainDb = s.HighShelfGainDb;
        CompressorEnabled = s.CompressorEnabled; ThresholdDb = s.ThresholdDb; Ratio = s.Ratio; MakeupGainDb = s.MakeupGainDb;
        NormalizeLoudness = s.NormalizeLoudness; TargetLufs = s.TargetLufs;
        _lowShelfHz = s.LowShelfHz; _midHz = s.MidHz; _midQ = s.MidQ; _highShelfHz = s.HighShelfHz;
        _attackMs = s.AttackMs; _releaseMs = s.ReleaseMs;
        Status = $"Profil „{value}“ angewendet.";
    }

    private MasteringSettings BuildSettings() => new()
    {
        HighPassEnabled = HighPassEnabled, HighPassHz = HighPassHz,
        LowShelfHz = _lowShelfHz, LowShelfGainDb = BassGainDb,
        MidHz = _midHz, MidQ = _midQ, MidGainDb = MidGainDb,
        HighShelfHz = _highShelfHz, HighShelfGainDb = TrebleGainDb,
        CompressorEnabled = CompressorEnabled, ThresholdDb = ThresholdDb, Ratio = Ratio,
        AttackMs = _attackMs, ReleaseMs = _releaseMs, MakeupGainDb = MakeupGainDb,
        NormalizeLoudness = NormalizeLoudness, TargetLufs = TargetLufs
    };

    private async Task<string> RenderSourceAsync()
    {
        var (samples, sr) = await _timeline.RenderTrackAsync(_track!);
        var dir = Path.Combine(Path.GetTempPath(), "Audiola", "trackmaster");
        Directory.CreateDirectory(dir);
        var temp = Path.Combine(dir, $"src_{Guid.NewGuid():N}.wav");
        AudioExporter.Export(new FloatArraySampleProvider(samples, sr, 2), temp);
        return temp;
    }

    private static string MasteredTempPath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "Audiola", "trackmaster");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"mastered_{Guid.NewGuid():N}.wav");
    }

    /// <summary>Rendert Original + gemasterte Fassung und lädt sie in den A-B-Vergleichsplayer.</summary>
    [RelayCommand]
    private async Task PreviewAsync()
    {
        if (_track is null || IsBusy) return;
        IsBusy = true; Status = "Erzeuge Vorschau …";
        try
        {
            _origCache ??= await RenderSourceAsync();              // Original nur einmal rendern
            var mastered = MasteredTempPath();
            await _mastering.ProcessAndExportAsync(_origCache, mastered, BuildSettings());
            _preview.Load(_origCache, mastered);
            _preview.ShowB = ShowMastered;
            PreviewLoaded = true;
            UpdatePreviewTime();
            Status = "Vorschau bereit — Play drücken und mit A/B vergleichen.";
        }
        catch (Exception ex) { UiError.Show("Vorschau fehlgeschlagen", ex.Message); Status = ""; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void PlayPausePreview()
    {
        if (PreviewLoaded) _preview.TogglePlay();
    }

    private void UpdatePreviewTime()
    {
        static string F(TimeSpan t) => $"{(int)t.TotalMinutes}:{t.Seconds:D2}";
        PreviewTime = $"{F(_preview.Position)} / {F(_preview.Duration)}";
    }

    /// <summary>Vom Dialog beim Schließen aufgerufen — gibt Audio-Ressourcen frei.</summary>
    public void StopPreview() => _preview.Dispose();

    /// <summary>Spur mastern und im Studio durch das Ergebnis ersetzen.</summary>
    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (_track is null || IsBusy) return;
        IsBusy = true; Status = "Mastere Spur …";
        try
        {
            var src = await RenderSourceAsync();
            var outp = MasteredTempPath();
            var result = await _mastering.ProcessAndExportAsync(src, outp, BuildSettings());
            await _timeline.ApplyProcessedTrackAsync(_track, outp);

            Status = $"Fertig: {result.InputLufs:F1} → {result.OutputLufs:F1} LUFS";
            _snackbar.Show("Spur gemastert", $"{_track.Name} aktualisiert.",
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(3));
            RequestClose?.Invoke();
        }
        catch (Exception ex) { UiError.Show("Mastern fehlgeschlagen", ex.Message); Status = ""; }
        finally { IsBusy = false; }
    }

    /// <summary>Gemasterte Spur über den Export-Dialog (Format/Tags/Lyrics) als Datei speichern.</summary>
    [RelayCommand]
    private async Task ExportAsync()
    {
        if (_track is null || IsBusy) return;
        var settings = BuildSettings();
        var meta = _songMeta.ToMetadata();
        if (string.IsNullOrWhiteSpace(meta.Title)) meta.Title = _track.Name;

        await _export.ExportAsync(
            _track.Name,
            async () =>
            {
                var src = await RenderSourceAsync();
                var outp = MasteredTempPath();
                await _mastering.ProcessAndExportAsync(src, outp, settings);
                return (ISampleProvider)new AudioFileReader(outp);
            },
            meta);
    }
}
