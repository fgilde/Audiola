using System.IO;
using Audiola.Models;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Audiola.ViewModels;

public sealed partial class MasteringViewModel : ObservableObject
{
    private readonly IMasteringService _mastering;
    private readonly ISnackbarService _snackbar;
    private readonly TimelineViewModel _timeline;
    private readonly StemMixerEngine _engine;
    private readonly LiveMasterProcessor _liveMaster;
    private readonly ISettingsService _settings;

    private static readonly string MasterDir = Path.Combine(Path.GetTempPath(), "Audiola", "master");
    private string? _sourcePath;
    private double _inputLufs = double.NegativeInfinity;

    public SessionState Session { get; }
    public TransportViewModel Transport { get; }

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = "Quelle wird vorbereitet …";
    [ObservableProperty] private string _sourceLabel = "";
    [ObservableProperty] private bool _previewMaster;
    [ObservableProperty] private IReadOnlyList<float> _mixPeaks = [];

    private float[] _originalPeaks = [];
    private float[]? _masteredPeaks;

    private void RefreshWaveform()
        => MixPeaks = PreviewMaster && _masteredPeaks is not null ? _masteredPeaks : _originalPeaks;

    private async Task UpdateMasteredWaveformAsync()
    {
        if (PreviewMaster && _sourcePath is not null)
        {
            try { _masteredPeaks = await _mastering.ProcessPeaksAsync(_sourcePath, BuildSettings()); }
            catch { _masteredPeaks = null; }
        }
        RefreshWaveform();
    }

    // EQ
    [ObservableProperty] private bool _highPassEnabled = true;
    [ObservableProperty] private double _highPassHz = 30;
    [ObservableProperty] private double _lowShelfGainDb;
    [ObservableProperty] private double _midGainDb;
    [ObservableProperty] private double _highShelfGainDb;

    // Kompressor
    [ObservableProperty] private bool _compressorEnabled = true;
    [ObservableProperty] private double _thresholdDb = -18;
    [ObservableProperty] private double _ratio = 2.0;
    [ObservableProperty] private double _makeupGainDb;

    // Loudness
    [ObservableProperty] private bool _normalizeLoudness = true;
    [ObservableProperty] private double _targetLufs = -14;

    // Erweiterte Parameter (von Profilen gesetzt; aktuell ohne eigene Slider).
    [ObservableProperty] private double _lowShelfHz = 100;
    [ObservableProperty] private double _midHz = 1000;
    [ObservableProperty] private double _midQ = 1.0;
    [ObservableProperty] private double _highShelfHz = 10000;
    [ObservableProperty] private double _attackMs = 10;
    [ObservableProperty] private double _releaseMs = 150;

    // Profile
    [ObservableProperty] private string? _selectedProfile;
    [ObservableProperty] private string _newProfileName = "";
    public System.Collections.ObjectModel.ObservableCollection<string> ProfileNames { get; } = [];

    public MasteringViewModel(SessionState session, IMasteringService mastering, ISnackbarService snackbar,
        TimelineViewModel timeline, StemMixerEngine engine, LiveMasterProcessor liveMaster, TransportViewModel transport,
        ISettingsService settings)
    {
        Session = session;
        _mastering = mastering;
        _snackbar = snackbar;
        _timeline = timeline;
        _engine = engine;
        _liveMaster = liveMaster;
        Transport = transport;
        _settings = settings;
        RebuildProfileNames();
    }

    private void RebuildProfileNames()
    {
        ProfileNames.Clear();
        foreach (var p in MasteringProfiles.All) ProfileNames.Add(p.Name);
        foreach (var u in _settings.Current.UserMasteringProfiles) ProfileNames.Add(u.Name);
    }

    private bool IsUserProfile(string? name) =>
        name is not null && _settings.Current.UserMasteringProfiles.Any(u => u.Name == name);

    [RelayCommand]
    private void SaveProfile()
    {
        var name = NewProfileName?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        if (MasteringProfiles.All.Any(p => p.Name == name))
        {
            _snackbar.Show("Name belegt", "Dieser Name ist ein eingebautes Profil.", ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.Warning24), TimeSpan.FromSeconds(3));
            return;
        }

        var list = _settings.Current.UserMasteringProfiles;
        var existing = list.FirstOrDefault(u => u.Name == name);
        if (existing is not null) existing.Settings = BuildSettings();
        else list.Add(new SavedMasteringProfile { Name = name, Settings = BuildSettings() });
        _settings.Save();

        RebuildProfileNames();
        SelectedProfile = name;
        NewProfileName = "";
        DeleteProfileCommand.NotifyCanExecuteChanged();
        _snackbar.Show("Profil gespeichert", name, ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(2));
    }

    private bool CanDeleteProfile => IsUserProfile(SelectedProfile);

    [RelayCommand(CanExecute = nameof(CanDeleteProfile))]
    private void DeleteProfile()
    {
        var u = _settings.Current.UserMasteringProfiles.FirstOrDefault(x => x.Name == SelectedProfile);
        if (u is null) return;
        _settings.Current.UserMasteringProfiles.Remove(u);
        _settings.Save();
        RebuildProfileNames();
        SelectedProfile = null;
        DeleteProfileCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Beim Anzeigen: den aktiven Studio-Mix rendern, Wellenform + LUFS bestimmen.</summary>
    public async Task PrepareFromStudioAsync()
    {
        if (_timeline.Tracks.Count > 0 && _timeline.DurationSeconds > 0.01)
        {
            IsBusy = true;
            StatusText = "Rendere Studio-Mix …";
            UpdateCommands();
            var dur = TimeSpan.FromSeconds(_timeline.DurationSeconds);
            var tracks = _timeline.Tracks.ToList();
            try
            {
                var (temp, peaks, lufs) = await Task.Run(() =>
                {
                    var (samples, sr) = _engine.RenderRange(tracks, TimeSpan.Zero, dur);
                    Directory.CreateDirectory(MasterDir);
                    var t = Path.Combine(MasterDir, $"mix_{Guid.NewGuid():N}.wav");
                    AudioEdits.WriteWav(t, samples, sr);
                    return (t, AudioEdits.ComputePeaks(samples), Dsp.LoudnessMeter.MeasureIntegratedLufs(samples, sr));
                });
                _sourcePath = temp;
                _originalPeaks = peaks;
                _masteredPeaks = null;
                _inputLufs = lufs;
                SourceLabel = $"Studio-Mix ({tracks.Count} aktive Spuren)";
            }
            catch (Exception ex)
            {
                _sourcePath = null;
                SourceLabel = "Render-Fehler: " + ex.Message;
            }
            IsBusy = false;
        }
        else if (Session.CurrentTrack is not null)
        {
            _sourcePath = Session.CurrentTrack.FilePath;
            _originalPeaks = Session.CurrentTrack.Peaks;
            _masteredPeaks = null;
            _inputLufs = await _mastering.MeasureLufsAsync(_sourcePath);
            SourceLabel = "Datei: " + Session.CurrentTrack.FileName;
        }
        else
        {
            _sourcePath = null;
            _originalPeaks = [];
            _masteredPeaks = null;
            SourceLabel = "Keine Quelle — im Studio Spuren laden.";
        }

        PushMaster();
        await UpdateMasteredWaveformAsync();
        StatusText = _sourcePath is null ? SourceLabel : $"Quelle: {SourceLabel}";
        UpdateCommands();
    }

    /// <summary>Aktuelle Einstellungen + Loudness-Gain in die Live-Vorschau schieben.</summary>
    private void PushMaster()
    {
        var gainDb = NormalizeLoudness && !double.IsInfinity(_inputLufs) ? TargetLufs - _inputLufs : 0;
        _liveMaster.SetSettings(BuildSettings(), gainDb);
        _liveMaster.Enabled = PreviewMaster;
    }

    partial void OnPreviewMasterChanged(bool value)
    {
        PushMaster();
        StatusText = value ? "Vorschau: Master AN (A/B – Schalter zum Vergleichen)" : "Vorschau: Master AUS (Original).";
        _ = UpdateMasteredWaveformAsync();
    }

    /// <summary>Aktuelle Mastering-Parameter + gewähltes Profil für die Projektdatei.</summary>
    public (MasteringSettings Settings, string? Profile) ExportSettings() => (BuildSettings(), SelectedProfile);

    /// <summary>Mastering-Parameter aus einem Projekt übernehmen.</summary>
    public void ImportSettings(MasteringSettings? s, string? profile)
    {
        if (s is null) return;
        SelectedProfile = profile; // setzt ggf. die Profil-Basis …
        // … exakte gespeicherte Werte überschreiben das Profil:
        HighPassEnabled = s.HighPassEnabled; HighPassHz = s.HighPassHz;
        LowShelfHz = s.LowShelfHz; LowShelfGainDb = s.LowShelfGainDb;
        MidHz = s.MidHz; MidQ = s.MidQ; MidGainDb = s.MidGainDb;
        HighShelfHz = s.HighShelfHz; HighShelfGainDb = s.HighShelfGainDb;
        CompressorEnabled = s.CompressorEnabled; ThresholdDb = s.ThresholdDb; Ratio = s.Ratio;
        AttackMs = s.AttackMs; ReleaseMs = s.ReleaseMs; MakeupGainDb = s.MakeupGainDb;
        NormalizeLoudness = s.NormalizeLoudness; TargetLufs = s.TargetLufs;
    }

    /// <summary>Seite verlassen: Live-Master aus.</summary>
    public void OnDeactivated()
    {
        PreviewMaster = false;
        _liveMaster.Enabled = false;
    }

    private void UpdateCommands()
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
    }

    private MasteringSettings BuildSettings() => new()
    {
        HighPassEnabled = HighPassEnabled,
        HighPassHz = HighPassHz,
        LowShelfHz = LowShelfHz,
        LowShelfGainDb = LowShelfGainDb,
        MidHz = MidHz,
        MidQ = MidQ,
        MidGainDb = MidGainDb,
        HighShelfHz = HighShelfHz,
        HighShelfGainDb = HighShelfGainDb,
        CompressorEnabled = CompressorEnabled,
        ThresholdDb = ThresholdDb,
        Ratio = Ratio,
        AttackMs = AttackMs,
        ReleaseMs = ReleaseMs,
        MakeupGainDb = MakeupGainDb,
        NormalizeLoudness = NormalizeLoudness,
        TargetLufs = TargetLufs
    };

    partial void OnSelectedProfileChanged(string? value)
    {
        DeleteProfileCommand.NotifyCanExecuteChanged();

        var s = MasteringProfiles.All.FirstOrDefault(p => p.Name == value)?.Settings
                ?? _settings.Current.UserMasteringProfiles.FirstOrDefault(u => u.Name == value)?.Settings;
        if (s is null) return;

        HighPassEnabled = s.HighPassEnabled;
        HighPassHz = s.HighPassHz;
        LowShelfHz = s.LowShelfHz;
        LowShelfGainDb = s.LowShelfGainDb;
        MidHz = s.MidHz;
        MidQ = s.MidQ;
        MidGainDb = s.MidGainDb;
        HighShelfHz = s.HighShelfHz;
        HighShelfGainDb = s.HighShelfGainDb;
        CompressorEnabled = s.CompressorEnabled;
        ThresholdDb = s.ThresholdDb;
        Ratio = s.Ratio;
        AttackMs = s.AttackMs;
        ReleaseMs = s.ReleaseMs;
        MakeupGainDb = s.MakeupGainDb;
        NormalizeLoudness = s.NormalizeLoudness;
        TargetLufs = s.TargetLufs;

        StatusText = $"Profil „{value}“ angewendet.";
        PushMaster();
    }

    partial void OnHighPassEnabledChanged(bool value) => PushMaster();
    partial void OnHighPassHzChanged(double value) => PushMaster();
    partial void OnLowShelfGainDbChanged(double value) => PushMaster();
    partial void OnMidGainDbChanged(double value) => PushMaster();
    partial void OnHighShelfGainDbChanged(double value) => PushMaster();
    partial void OnCompressorEnabledChanged(bool value) => PushMaster();
    partial void OnThresholdDbChanged(double value) => PushMaster();
    partial void OnRatioChanged(double value) => PushMaster();
    partial void OnMakeupGainDbChanged(double value) => PushMaster();
    partial void OnNormalizeLoudnessChanged(bool value) => PushMaster();
    partial void OnTargetLufsChanged(double value) => PushMaster();

    private bool CanRun => _sourcePath is not null && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task AnalyzeAsync()
    {
        if (_sourcePath is null) return;
        IsBusy = true;
        AnalyzeCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
        try
        {
            StatusText = "Messe Lautheit …";
            var lufs = await _mastering.MeasureLufsAsync(_sourcePath);
            StatusText = double.IsInfinity(lufs)
                ? "Signal zu leise/kurz für eine LUFS-Messung."
                : $"Eingang: {lufs:F1} LUFS (Ziel: {TargetLufs:F0} LUFS).";
        }
        catch (Exception ex)
        {
            StatusText = "Fehler: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
            AnalyzeCommand.NotifyCanExecuteChanged();
            ExportCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ExportAsync()
    {
        if (_sourcePath is null) return;

        var dialog = new SaveFileDialog
        {
            Title = "Gemastertes Audio exportieren",
            Filter = AudioExporter.SaveFilter,
            FileName = "studio-master.wav"
        };
        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        AnalyzeCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
        try
        {
            StatusText = "Verarbeite & exportiere …";
            var result = await _mastering.ProcessAndExportAsync(
                _sourcePath, dialog.FileName, BuildSettings());

            StatusText = $"Fertig: {result.InputLufs:F1} → {result.OutputLufs:F1} LUFS " +
                         $"(Gain {result.AppliedGainDb:+0.0;-0.0} dB)" +
                         (result.ClippedSamples > 0 ? $", {result.ClippedSamples} Clipping-Samples" : "");

            _snackbar.Show("Mastering fertig", Path.GetFileName(dialog.FileName),
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            StatusText = "Fehler: " + ex.Message;
            _snackbar.Show("Mastering fehlgeschlagen", ex.Message,
                ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(5));
        }
        finally
        {
            IsBusy = false;
            AnalyzeCommand.NotifyCanExecuteChanged();
            ExportCommand.NotifyCanExecuteChanged();
        }
    }
}
