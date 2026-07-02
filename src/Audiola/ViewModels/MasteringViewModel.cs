using System.IO;
using System.Windows;
using System.Windows.Media;
using Audiola.Dsp;
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

    private string? _sourcePath;
    private double _inputLufs = double.NegativeInfinity;

    // Spur-Modus („Spur mastern"-Dialog nutzt dasselbe Panel/VM mit einer Einzelspur als Quelle).
    private StemTrackViewModel? _trackTarget;
    private List<(StemTrackViewModel Track, bool Solo, bool Mute)>? _soloSnapshot;

    /// <summary>True, wenn eine einzelne Spur (statt des Studio-Mixes) die Quelle ist.</summary>
    [ObservableProperty] private bool _isTrackSource;

    /// <summary>Vom „Spur mastern"-Dialog abonniert — nach dem Anwenden schließen.</summary>
    public event Action? CloseDialogRequested;

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

    // Stapelverarbeitung (Bulk)
    [ObservableProperty] private bool _bulkOverwrite;
    [ObservableProperty] private string _bulkPattern = "{name}_mastered";
    [ObservableProperty] private bool _isBulkRunning;
    [ObservableProperty] private string _bulkStatus = "";

    // Ausgabeformat für die Stapelverarbeitung (z. B. MP3 → WAV konvertieren).
    public IReadOnlyList<string> BulkFormats { get; } =
        ["Wie Original", "WAV (.wav)", "MP3 (.mp3)", "AAC / M4A (.m4a)"];
    [ObservableProperty] private string _bulkFormat = "Wie Original";

    private static string? ExtForFormat(string fmt) => fmt switch
    {
        "WAV (.wav)" => ".wav",
        "MP3 (.mp3)" => ".mp3",
        "AAC / M4A (.m4a)" => ".m4a",
        _ => null // Wie Original
    };

    // Live-EQ-Kurve (Magnitudengang im 1000×200-Koordinatenraum für die Vorschau).
    [ObservableProperty] private PointCollection _eqCurve = new();

    // LUFS-Anzeige (0..1 über den Slider-Bereich −24..−6) für die Loudness-Lehre.
    [ObservableProperty] private double _inputLufsPercent;
    [ObservableProperty] private string _inputLufsLabel = "—";

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
        UpdateEqCurve();
    }

    /// <summary>Berechnet den kombinierten Magnitudengang (HP + Shelves + Mitte) für die Vorschaukurve.</summary>
    private void UpdateEqCurve()
    {
        const int sr = 44100, n = 160;
        const double w = 1000.0, h = 200.0, halfH = h / 2.0, span = halfH - 10.0;
        double logMin = Math.Log10(20), logMax = Math.Log10(20000);

        var chain = new List<Biquad>();
        if (HighPassEnabled) chain.Add(Biquad.HighPass(sr, HighPassHz, 0.707));
        chain.Add(Biquad.LowShelf(sr, LowShelfHz, 0.707, LowShelfGainDb));
        chain.Add(Biquad.Peaking(sr, MidHz, MidQ, MidGainDb));
        chain.Add(Biquad.HighShelf(sr, HighShelfHz, 0.707, HighShelfGainDb));

        var pts = new PointCollection(n + 1);
        for (var i = 0; i <= n; i++)
        {
            double t = (double)i / n;
            double f = Math.Pow(10, logMin + t * (logMax - logMin));
            double db = 0;
            foreach (var b in chain) db += b.MagnitudeDb(f, sr);
            double y = halfH - Math.Clamp(db, -15, 15) / 15.0 * span;
            pts.Add(new Point(t * w, y));
        }
        pts.Freeze();
        EqCurve = pts;
    }

    /// <summary>Aktualisiert die LUFS-Lehre (Eingangslautheit relativ zum Slider-Bereich).</summary>
    private void UpdateLufsGauge()
    {
        if (double.IsInfinity(_inputLufs))
        {
            InputLufsPercent = 0;
            InputLufsLabel = "—";
            return;
        }
        InputLufsPercent = Math.Clamp((_inputLufs - (-24)) / ((-6) - (-24)), 0, 1);
        InputLufsLabel = $"{_inputLufs:F1} LUFS";
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
            _snackbar.Warning("Name belegt", "Dieser Name ist ein eingebautes Profil.");
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
        _snackbar.Success("Profil gespeichert", name, 2);
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
                    var t = TempDir.File("master", ".wav", "mix");
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

    /// <summary>
    /// Quelle auf eine einzelne Studio-Spur stellen („Spur mastern"): Spur rendern (Wellenform + LUFS)
    /// und für die hörbare Live-Vorschau temporär solo schalten — der A/B-Schalter, Seek und alle
    /// Regler funktionieren damit exakt wie beim Mastern des ganzen Mixes.
    /// </summary>
    public async Task PrepareFromTrackAsync(StemTrackViewModel track)
    {
        IsBusy = true;
        StatusText = $"Rendere Spur „{track.Name}“ …";
        UpdateCommands();

        _trackTarget = track;
        IsTrackSource = true;

        // Solo-Zustand sichern und die Spur isolieren (hörbare Vorschau = nur diese Spur).
        _soloSnapshot = _timeline.Tracks.Select(t => (t, t.IsSolo, t.IsMuted)).ToList();
        foreach (var t in _timeline.Tracks) t.IsSolo = false;
        track.IsSolo = true;
        track.IsMuted = false;

        try
        {
            var (temp, peaks, lufs) = await Task.Run(async () =>
            {
                var (samples, sr) = await _timeline.RenderTrackAsync(track);
                var t = TempDir.File("trackmaster", ".wav", "src");
                AudioEdits.WriteWav(t, samples, sr);
                return (t, AudioEdits.ComputePeaks(samples), Dsp.LoudnessMeter.MeasureIntegratedLufs(samples, sr));
            });
            _sourcePath = temp;
            _originalPeaks = peaks;
            _masteredPeaks = null;
            _inputLufs = lufs;
            SourceLabel = $"Spur: {track.Name}";
        }
        catch (Exception ex)
        {
            _sourcePath = null;
            SourceLabel = "Render-Fehler: " + ex.Message;
        }
        IsBusy = false;

        PushMaster();
        await UpdateMasteredWaveformAsync();
        StatusText = _sourcePath is null ? SourceLabel : $"Quelle: {SourceLabel}";
        UpdateCommands();
    }

    /// <summary>Spur-Modus beenden (Dialog zu): Solo/Mute wiederherstellen, Live-Master aus.</summary>
    public void EndTrackPreview()
    {
        if (_soloSnapshot is not null)
            foreach (var (t, solo, mute) in _soloSnapshot) { t.IsSolo = solo; t.IsMuted = mute; }
        _soloSnapshot = null;
        _trackTarget = null;
        IsTrackSource = false;
        OnDeactivated();
    }

    /// <summary>Spur mastern und im Studio durch das Ergebnis ersetzen (mit Undo).</summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ApplyToTrackAsync()
    {
        if (_trackTarget is null || _sourcePath is null) return;
        IsBusy = true;
        StatusText = "Mastere Spur …";
        try
        {
            var outp = TempDir.File("trackmaster", ".wav", "mastered");
            var result = await _mastering.ProcessAndExportAsync(_sourcePath, outp, BuildSettings());
            await _timeline.ApplyProcessedTrackAsync(_trackTarget, outp);
            _snackbar.Success("Spur gemastert",
                $"{_trackTarget.Name}: {result.InputLufs:F1} → {result.OutputLufs:F1} LUFS");
            CloseDialogRequested?.Invoke();
        }
        catch (Exception ex) { UiError.Show("Spur mastern fehlgeschlagen", ex.Message); }
        finally { IsBusy = false; }
    }

    /// <summary>Aktuelle Einstellungen + Loudness-Gain in die Live-Vorschau schieben.</summary>
    private void PushMaster()
    {
        var gainDb = NormalizeLoudness && !double.IsInfinity(_inputLufs) ? TargetLufs - _inputLufs : 0;
        _liveMaster.SetSettings(BuildSettings(), gainDb);
        _liveMaster.Enabled = PreviewMaster;
        UpdateEqCurve();
        UpdateLufsGauge();
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
        ApplyToTrackCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Mastert mehrere ausgewählte Dateien mit den aktuell aktiven Einstellungen.
    /// Pattern (<c>{name}</c>) bildet den Ausgabenamen; optional werden die Originale überschrieben.
    /// </summary>
    [RelayCommand]
    private async Task BulkMasterAsync()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Dateien für Bulk-Mastering wählen",
            Multiselect = true,
            Filter = "Audiodateien|*.wav;*.mp3;*.flac;*.aiff;*.m4a;*.ogg|Alle Dateien|*.*"
        };
        if (dlg.ShowDialog() != true || dlg.FileNames.Length == 0) return;

        var files = dlg.FileNames;
        var settings = BuildSettings();
        var overwrite = BulkOverwrite;
        var pattern = string.IsNullOrWhiteSpace(BulkPattern) ? "{name}_mastered" : BulkPattern.Trim();
        var chosenExt = ExtForFormat(BulkFormat); // null = Endung der Quelldatei beibehalten

        IsBulkRunning = true;
        int ok = 0, fail = 0;
        try
        {
            for (var i = 0; i < files.Length; i++)
            {
                var input = files[i];
                BulkStatus = $"Mastere {i + 1}/{files.Length}: {Path.GetFileName(input)} …";
                try
                {
                    var dir = Path.GetDirectoryName(input)!;
                    var ext = chosenExt ?? Path.GetExtension(input);
                    var baseName = Path.GetFileNameWithoutExtension(input);
                    var outPath = overwrite
                        ? Path.Combine(dir, baseName + ext)
                        : Path.Combine(dir, pattern.Replace("{name}", baseName) + ext);

                    // Wenn Ziel = Quelle: über eine temporäre Datei schreiben (Original nicht korrumpieren).
                    if (string.Equals(outPath, input, StringComparison.OrdinalIgnoreCase))
                    {
                        var tmp = TempDir.File("bulk", ext);
                        await _mastering.ProcessAndExportAsync(input, tmp, settings);
                        File.Copy(tmp, input, overwrite: true);
                        try { File.Delete(tmp); } catch { /* egal */ }
                    }
                    else
                    {
                        await _mastering.ProcessAndExportAsync(input, outPath, settings);
                    }
                    ok++;
                }
                catch { fail++; }
            }

            BulkStatus = $"Fertig: {ok} gemastert{(fail > 0 ? $", {fail} fehlgeschlagen" : "")}.";
            _snackbar.Show("Bulk-Mastering fertig", BulkStatus,
                fail == 0 ? ControlAppearance.Success : ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(5));
        }
        finally
        {
            IsBulkRunning = false;
        }
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

        var src = _sourcePath;
        var settings = BuildSettings();
        var meta = Audiola.App.GetService<SongMetadata>().ToMetadata();
        var name = string.IsNullOrWhiteSpace(meta.Title) ? "studio-master" : meta.Title!;

        await Audiola.App.GetService<Audiola.Services.ExportService>().ExportAsync(
            name,
            async () =>
            {
                StatusText = "Verarbeite & exportiere …";
                var tempWav = TempDir.File("master", ".wav", "master");
                var result = await _mastering.ProcessAndExportAsync(src, tempWav, settings);
                StatusText = $"Fertig: {result.InputLufs:F1} → {result.OutputLufs:F1} LUFS " +
                             $"(Gain {result.AppliedGainDb:+0.0;-0.0} dB)" +
                             (result.ClippedSamples > 0 ? $", {result.ClippedSamples} Clipping-Samples" : "");
                return (NAudio.Wave.ISampleProvider)new NAudio.Wave.AudioFileReader(tempWav);
            },
            meta);
    }
}
