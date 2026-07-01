using System.Collections.ObjectModel;
using System.IO;
using Audiola.Dsp;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Audiola.ViewModels;

/// <summary>
/// Einsing-Studio: singt zu einem Backing-Track (gewählte Spuren, Original-Gesang stummgeschaltet),
/// zeigt die Lyrics synchron, gibt SingStar-artiges Ton-Feedback gegen die Referenz-Melodie
/// (aus der Gesangsspur) und übernimmt die Aufnahme am Ende als neue Studio-Spur. Abschnitte lassen
/// sich per Loop-Region einzeln neu einsingen (Comping).
/// </summary>
public sealed partial class SingAlongViewModel : ObservableObject, IDisposable
{
    private readonly TimelineViewModel _timeline;
    private readonly ISettingsService _settings;
    private readonly SongMetadata _songMeta;
    private readonly ISnackbarService _snackbar;

    private readonly VocalRecordingEngine _engine = new();
    private readonly LatencyCalibrator _calibrator = new();

    private PitchPoint[] _reference = [];              // Soll-Melodie (Hz über Zeit)
    private readonly List<PitchSample> _sung = [];      // live gesungener Verlauf (fürs Band)
    private string? _backingPath;

    public SingAlongViewModel(TimelineViewModel timeline, ISettingsService settings,
        SongMetadata songMeta, ISnackbarService snackbar)
    {
        _timeline = timeline;
        _settings = settings;
        _songMeta = songMeta;
        _snackbar = snackbar;
        _engine.LatencySeconds = settings.Current.VocalLatencyMs / 1000.0;
        LatencyMs = settings.Current.VocalLatencyMs;

        _engine.PositionChanged += (_, _) => OnPosition();
        _engine.Pitch += OnPitchDetected;
        _engine.PlaybackEnded += (_, _) => OnPlaybackEnded();
    }

    // ---- Spuren ----
    public ObservableCollection<StemTrackViewModel> Tracks { get; } = [];
    [ObservableProperty] private StemTrackViewModel? _referenceTrack;   // liefert die Soll-Melodie

    // ---- Mikrofon ----
    public ObservableCollection<MicDevice> Mics { get; } = [];
    [ObservableProperty] private MicDevice? _selectedMic;

    partial void OnSelectedMicChanged(MicDevice? value)
    {
        if (value is not null) _engine.DeviceNumber = value.Index;
    }

    // ---- Status ----
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "Backing vorbereiten und Latenz kalibrieren.";
    [ObservableProperty] private bool _prepared;
    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private double _positionSeconds;
    [ObservableProperty] private double _durationSeconds;
    [ObservableProperty] private string _positionText = "0:00 / 0:00";

    private static string Fmt(double s) => $"{(int)(s / 60)}:{(int)(Math.Max(0, s) % 60):D2}";

    // ---- Latenz ----
    [ObservableProperty] private double _latencyMs;
    [ObservableProperty] private string _calibrationStatus = "";

    partial void OnLatencyMsChanged(double value)
    {
        _engine.LatencySeconds = value / 1000.0;
        _settings.Current.VocalLatencyMs = value;
        _settings.Save();
    }

    // ---- Loop-Region (Punch-in) ----
    [ObservableProperty] private bool _loopEnabled;
    [ObservableProperty] private double _loopStartSeconds;
    [ObservableProperty] private double _loopEndSeconds;

    // ---- Lyrics ----
    public ObservableCollection<LyricLineViewModel> LyricLines { get; } = [];
    [ObservableProperty] private bool _hasLyrics;

    /// <summary>Nur dann Lyrics extrahieren anbieten, wenn (noch) keine vorhanden sind.</summary>
    public bool NeedsLyrics => !HasLyrics;
    partial void OnHasLyricsChanged(bool value) => OnPropertyChanged(nameof(NeedsLyrics));
    [ObservableProperty] private string _lyricPrev = "";
    [ObservableProperty] private string _lyricCurrent = "";
    [ObservableProperty] private string _lyricNext = "";
    private IReadOnlyList<LyricLine> _lyrics = [];
    private int _curLyricIndex = -1;

    // ---- Pitch-Feedback / Score ----
    [ObservableProperty] private double _sungHz;
    [ObservableProperty] private double _targetHz;
    [ObservableProperty] private bool _isOnPitch;
    [ObservableProperty] private string _sungNote = "–";
    [ObservableProperty] private string _targetNote = "–";
    [ObservableProperty] private int _scorePercent;
    [ObservableProperty] private int _points;
    private int _hits, _judged;

    public IReadOnlyList<PitchPoint> Reference => _reference;
    public IReadOnlyList<PitchSample> SungHistory => _sung;
    public event EventHandler? PitchUpdated;   // fürs Pitch-Band-Control

    /// <summary>Beim Öffnen aufgerufen: Spuren übernehmen und Lyrics einlesen.</summary>
    public void Initialize()
    {
        Tracks.Clear();
        foreach (var t in _timeline.Tracks) Tracks.Add(t);

        // Verfügbare Mikrofone auflisten.
        Mics.Clear();
        for (int i = 0; i < NAudio.Wave.WaveInEvent.DeviceCount; i++)
            Mics.Add(new MicDevice(i, NAudio.Wave.WaveInEvent.GetCapabilities(i).ProductName));
        SelectedMic = Mics.FirstOrDefault();
        if (Mics.Count == 0) Status = "Kein Mikrofon gefunden — bitte ein Aufnahmegerät anschließen.";

        // Gesangs-Spur raten (Name enthält „vocal"/„gesang"/„stimme"/„lead").
        ReferenceTrack = Tracks.FirstOrDefault(t =>
            t.Name.Contains("vocal", StringComparison.OrdinalIgnoreCase) ||
            t.Name.Contains("gesang", StringComparison.OrdinalIgnoreCase) ||
            t.Name.Contains("stimme", StringComparison.OrdinalIgnoreCase) ||
            t.Name.Contains("lead", StringComparison.OrdinalIgnoreCase)) ?? Tracks.FirstOrDefault();

        DurationSeconds = _timeline.DurationSeconds;
        LoadLyrics(_songMeta.Lyrics);   // vorhandene Lyrics aus den Metadaten übernehmen
    }

    private void LoadLyrics(string? lrc)
    {
        _lyrics = LrcParser.Parse(lrc);

        // Lyrics ohne Zeitmarken (Fließtext aus den Metadaten) grob über die Songdauer verteilen,
        // damit sie trotzdem mitlaufen — keine erneute Extraktion nötig.
        if (_lyrics.Count == 0 && !string.IsNullOrWhiteSpace(lrc) && DurationSeconds > 1)
        {
            var lines = lrc.Replace("\r\n", "\n").Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith('['))   // Tag-Zeilen ([ti:]…) überspringen
                .ToList();
            if (lines.Count > 0)
            {
                double step = DurationSeconds / (lines.Count + 1);
                _lyrics = lines.Select((t, i) => new LyricLine(step * (i + 1), t)).ToList();
            }
        }

        HasLyrics = _lyrics.Count > 0;
        LyricLines.Clear();
        foreach (var l in _lyrics) LyricLines.Add(new LyricLineViewModel(l.TimeSeconds, l.Text));
        _curLyricIndex = -1;
        LyricPrev = LyricCurrent = LyricNext = "";
        if (HasLyrics)
            Status = LrcParser.HasTimestamps(lrc)
                ? "Lyrics geladen." : "Lyrics geladen (ohne Zeitmarken, grob verteilt).";
    }

    /// <summary>Lyrics aus der Gesangsspur (oder dem Mix) transkribieren und projektweit speichern.</summary>
    [RelayCommand]
    private async Task ExtractLyricsAsync()
    {
        if (IsBusy) return;
        IsBusy = true; Status = "Lyrics werden extrahiert …";
        try
        {
            string? source = ReferenceTrack?.Clips.FirstOrDefault()?.SourcePath;
            source ??= await _timeline.RenderMixToTempFileAsync();
            if (source is null) { Status = "Keine Audioquelle für die Lyrics."; return; }

            // ElevenLabs nutzen, falls ein Key hinterlegt ist — sonst lokal (Whisper).
            var useElevenLabs = _timeline.ElevenLabsAvailable;
            var lrc = await _timeline.TranscribeFileToLrcAsync(source,
                string.IsNullOrWhiteSpace(_songMeta.Title) ? null : _songMeta.Title, useElevenLabs);
            if (string.IsNullOrWhiteSpace(lrc)) { Status = "Es konnten keine Lyrics erkannt werden."; return; }

            _songMeta.Lyrics = lrc;   // projektweit speichern → nur einmal nötig
            LoadLyrics(lrc);
            Status = "Lyrics extrahiert und gespeichert.";
        }
        catch (Exception ex) { UiError.Show("Lyrics-Extraktion fehlgeschlagen", ex.Message); Status = ""; }
        finally { IsBusy = false; }
    }

    /// <summary>Backing rendern (gemuteter Gesang bleibt draußen) und Referenz-Melodie extrahieren.</summary>
    [RelayCommand]
    private async Task PrepareAsync()
    {
        if (IsBusy) return;
        IsBusy = true; Status = "Backing wird gerendert …";
        try
        {
            _backingPath = await _timeline.RenderMixToTempFileAsync();
            if (_backingPath is null) { Status = "Kein Backing (keine Spuren)."; return; }
            _engine.LoadBacking(_backingPath);
            DurationSeconds = _engine.Duration.TotalSeconds;
            if (LoopEndSeconds <= 0) LoopEndSeconds = DurationSeconds;

            Status = "Referenz-Melodie wird analysiert …";
            await BuildReferenceAsync();

            Prepared = true;
            Status = _reference.Length > 0
                ? "Bereit. Kopfhörer auf, Aufnahme starten."
                : "Bereit (ohne Referenz-Melodie — Gesangsspur wählen für Ton-Bewertung).";
        }
        catch (Exception ex) { UiError.Show("Vorbereitung fehlgeschlagen", ex.Message); Status = ""; }
        finally { IsBusy = false; }
    }

    private async Task BuildReferenceAsync()
    {
        _reference = [];
        var clip = ReferenceTrack?.Clips.FirstOrDefault();
        if (clip is null || string.IsNullOrEmpty(clip.SourcePath)) return;

        await Task.Run(() =>
        {
            var (stereo, sr) = AudioProcessingHelper.ReadStereo(clip.SourcePath);
            var mono = new float[stereo.Length / 2];
            for (int i = 0; i < mono.Length; i++) mono[i] = 0.5f * (stereo[2 * i] + stereo[2 * i + 1]);
            var pts = PitchDetector.Track(mono, sr);
            // Von Quell- auf Timeline-Zeit verschieben (Trim/Offset des Clips berücksichtigen).
            double shift = clip.TimelineOffsetSeconds - clip.SourceStartSeconds;
            _reference = pts.Select(p => p with { TimeSeconds = p.TimeSeconds + shift })
                            .Where(p => p.TimeSeconds >= 0).ToArray();
        });
    }

    // ---- Kalibrierung ----
    [RelayCommand]
    private async Task CalibrateAsync()
    {
        if (IsBusy) return;
        IsBusy = true; CalibrationStatus = "Messung läuft — bitte OHNE Kopfhörer, ruhig sein …";
        try
        {
            var ms = await _calibrator.MeasureMsAsync(SelectedMic?.Index ?? 0);
            LatencyMs = Math.Round(ms);
            CalibrationStatus = $"Gemessen: {LatencyMs:F0} ms Versatz.";
        }
        catch (Exception ex) { CalibrationStatus = ex.Message; }
        finally { IsBusy = false; }
    }

    // ---- Transport ----
    [RelayCommand]
    private void Play()
    {
        if (!Prepared) return;
        _sung.Clear();
        _engine.Play(StartPosition(), record: false);
    }

    [RelayCommand]
    private void Record()
    {
        if (!Prepared) return;
        ResetScore();
        _sung.Clear();
        _engine.Play(StartPosition(), record: true);
    }

    [RelayCommand]
    private void Stop() => _engine.Stop();

    private double StartPosition() => LoopEnabled ? LoopStartSeconds : PositionSeconds;

    private void OnPlaybackEnded()
    {
        // Kommt vom WaveOut-Thread → auf den UI-Thread marshallen.
        var disp = System.Windows.Application.Current?.Dispatcher;
        if (disp is not null && !disp.CheckAccess()) { disp.BeginInvoke(OnPlaybackEnded); return; }
        IsPlaying = false; IsRecording = false;
    }

    // ---- Übernahme ----
    [RelayCommand]
    private async Task AcceptAsync()
    {
        if (IsBusy) return;
        _engine.Stop();
        var buf = _engine.VocalBuffer;
        if (buf.Length == 0) { Status = "Noch nichts aufgenommen."; return; }

        IsBusy = true; Status = "Aufnahme wird übernommen …";
        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "Audiola", "vocal");
            Directory.CreateDirectory(dir);
            var wav = Path.Combine(dir, $"gesang_{Guid.NewGuid():N}.wav");
            AudioExporter.Export(new FloatArraySampleProvider(buf, _engine.SampleRate, 1), wav);
            await _timeline.AddAudioFileAsync(wav, -1, 0);

            _snackbar.Show("Gesang übernommen", "Als neue Spur ins Studio eingefügt.",
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(3));
            Status = "Übernommen.";
        }
        catch (Exception ex) { UiError.Show("Übernahme fehlgeschlagen", ex.Message); Status = ""; }
        finally { IsBusy = false; }
    }

    // ---- Engine-Events ----
    private void OnPosition()
    {
        PositionSeconds = _engine.Position.TotalSeconds;
        PositionText = $"{Fmt(PositionSeconds)} / {Fmt(DurationSeconds)}";
        IsPlaying = _engine.IsPlaying;
        IsRecording = _engine.IsRecording;

        // Loop-Region: am Ende zurückspringen bzw. stoppen.
        if (LoopEnabled && _engine.IsPlaying && PositionSeconds >= LoopEndSeconds)
        {
            bool rec = _engine.IsRecording;
            _engine.Play(LoopStartSeconds, rec);
        }

        UpdateLyric();
    }

    private void UpdateLyric()
    {
        if (_lyrics.Count == 0) return;
        int idx = -1;
        for (int i = 0; i < _lyrics.Count; i++)
            if (_lyrics[i].TimeSeconds <= PositionSeconds) idx = i; else break;
        if (idx == _curLyricIndex) return;
        _curLyricIndex = idx;
        LyricPrev = idx > 0 ? _lyrics[idx - 1].Text : "";
        LyricCurrent = idx >= 0 ? _lyrics[idx].Text : "";
        LyricNext = idx + 1 < _lyrics.Count ? _lyrics[idx + 1].Text : "";
    }

    private void OnPitchDetected(object? sender, PitchSample s)
    {
        // Läuft vom WaveIn-Thread → auf den UI-Thread marshallen (Properties sind gebunden).
        var disp = System.Windows.Application.Current?.Dispatcher;
        if (disp is not null && !disp.CheckAccess()) { disp.BeginInvoke(() => OnPitchDetected(sender, s)); return; }

        _sung.Add(s);
        SungHz = s.Hz;
        SungNote = s.Hz > 0 ? PitchDetector.MidiToName(PitchDetector.HzToMidi(s.Hz)) : "–";

        double tHz = ReferenceAt(s.TimeSeconds);
        TargetHz = tHz;
        TargetNote = tHz > 0 ? PitchDetector.MidiToName(PitchDetector.HzToMidi(tHz)) : "–";

        // Nur werten, wenn Referenz UND Gesang stimmhaft sind.
        if (tHz > 0 && s.Hz > 0 && s.Level > 0.01f)
        {
            double cents = PitchDetector.CentsOffOctaveless(s.Hz, tHz);
            bool hit = Math.Abs(cents) < 100; // innerhalb eines Halbtons
            IsOnPitch = hit;
            _judged++;
            if (hit) { _hits++; Points += 10; }
            ScorePercent = _judged > 0 ? (int)Math.Round(100.0 * _hits / _judged) : 0;
        }
        else IsOnPitch = false;

        PitchUpdated?.Invoke(this, EventArgs.Empty);
    }

    private double ReferenceAt(double t)
    {
        if (_reference.Length == 0) return 0;
        // binäre Suche auf TimeSeconds
        int lo = 0, hi = _reference.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (_reference[mid].TimeSeconds < t) lo = mid + 1; else hi = mid;
        }
        // nächstgelegenen Punkt nehmen, nur wenn zeitlich nah (±100 ms)
        var p = _reference[lo];
        if (Math.Abs(p.TimeSeconds - t) > 0.1 && lo > 0 &&
            Math.Abs(_reference[lo - 1].TimeSeconds - t) < Math.Abs(p.TimeSeconds - t)) p = _reference[lo - 1];
        return Math.Abs(p.TimeSeconds - t) <= 0.12 ? p.Hz : 0;
    }

    private void ResetScore()
    {
        _hits = _judged = 0; Points = 0; ScorePercent = 0;
    }

    public void Dispose() => _engine.Dispose();
}

/// <summary>Eine Lyric-Zeile für die Karaoke-Liste (Zeit + Text).</summary>
public sealed class LyricLineViewModel(double timeSeconds, string text)
{
    public double TimeSeconds { get; } = timeSeconds;
    public string Text { get; } = text;
}

/// <summary>Ein wählbares Aufnahme-Gerät (Mikrofon).</summary>
public sealed record MicDevice(int Index, string Name);
