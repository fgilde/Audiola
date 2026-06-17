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
/// Wellenform-Editor: Bereich auswählen und schneiden (Trim/Löschen/Stille/Fades),
/// mit Undo und Export. Arbeitet auf einem In-Memory-Stereo-Puffer; nach jeder
/// Änderung wird eine temporäre WAV geschrieben und in Sitzung + Player übernommen,
/// sodass die globale Transportleiste den bearbeiteten Stand abspielt.
/// </summary>
public sealed partial class EditorViewModel : ObservableObject
{
    private readonly SessionState _session;
    private readonly IAudioPlayerService _player;
    private readonly LiveFxProcessor _liveFx;
    private readonly ISnackbarService _snackbar;
    private readonly TimelineViewModel _timeline;
    private readonly INavigationService _navigation;
    private ClipViewModel? _targetClip; // gesetzt ⇒ wir bearbeiten einen Studio-Clip

    private static readonly string EditDir =
        Path.Combine(Path.GetTempPath(), "Audiola", "edits");

    private float[]? _buffer;
    private int _sampleRate;
    private string? _bufferPath;
    private string? _originalPath;
    private string? _previousTemp;
    private readonly Stack<float[]> _undo = new();

    public SessionState Session { get; }
    public TransportViewModel Transport { get; }

    [ObservableProperty] private string _formatInfo = "";
    [ObservableProperty] private string _pathInfo = "";
    [ObservableProperty] private string _displayName = "Keine Datei geladen";
    [ObservableProperty] private IReadOnlyList<float>? _peaks;
    [ObservableProperty] private string _selectionInfo = "Keine Auswahl — auf der Wellenform ziehen, um einen Bereich zu markieren.";
    [ObservableProperty] private double _selectionStart = double.NaN;
    [ObservableProperty] private double _selectionEnd = double.NaN;

    public bool HasTrack => _buffer is not null;
    public bool HasSelection => !double.IsNaN(SelectionStart) && !double.IsNaN(SelectionEnd) && SelectionEnd > SelectionStart;
    public bool CanUndo => _undo.Count > 0;

    public EditorViewModel(
        SessionState session,
        IAudioPlayerService player,
        TransportViewModel transport,
        LiveFxProcessor liveFx,
        ISnackbarService snackbar,
        TimelineViewModel timeline,
        INavigationService navigation)
    {
        _session = session;
        Session = session;
        _player = player;
        Transport = transport;
        _liveFx = liveFx;
        _snackbar = snackbar;
        _timeline = timeline;
        _navigation = navigation;

        Directory.CreateDirectory(EditDir);
    }

    public bool HasClipTarget => _targetClip is not null;

    /// <summary>Lädt einen Studio-Clip zum Detail-Bearbeiten (Doppelklick im Studio).</summary>
    public void LoadClipForEdit(ClipViewModel clip)
    {
        var (samples, sr) = AudioProcessingHelper.ReadStereo(clip.SourcePath);
        var startS = (int)Math.Clamp((long)(clip.SourceStartSeconds * sr) * 2, 0, samples.Length);
        var lenS = (int)Math.Clamp((long)(clip.LengthSeconds * sr) * 2, 0, samples.Length - startS);
        var seg = new float[lenS];
        Array.Copy(samples, startS, seg, 0, lenS);

        _buffer = seg;
        _sampleRate = sr;
        _targetClip = clip;
        _originalPath = clip.SourcePath;
        _undo.Clear();
        ClearSelection();
        Commit();                         // schreibt Vorschau-Temp + Player
        Transport.SetMode(TransportMode.Original); // Editor-Vorschau hörbar
        OnPropertyChanged(nameof(HasClipTarget));
        BakeToStudioCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Zurück ins Studio, ohne die Bearbeitung zu übernehmen (Zurück-Pfeil oben links).</summary>
    [RelayCommand]
    private void CloseEditor()
    {
        _targetClip = null;
        OnPropertyChanged(nameof(HasClipTarget));
        BakeToStudioCommand.NotifyCanExecuteChanged();
        _navigation.Navigate(typeof(Views.Pages.TimelinePage));
    }

    [RelayCommand(CanExecute = nameof(HasClipTarget))]
    private void BakeToStudio()
    {
        if (_targetClip is null || _buffer is null) return;
        _timeline.ReplaceClipFromBuffer(_targetClip, _buffer, _sampleRate);
        Notify("Ins Studio übernommen", "Clip aktualisiert.", true);
        _targetClip = null;
        OnPropertyChanged(nameof(HasClipTarget));
        BakeToStudioCommand.NotifyCanExecuteChanged();
        _navigation.Navigate(typeof(Views.Pages.TimelinePage));
    }

    // ---- Live-Effekt-Vorschau (wirkt beim Abspielen) ----

    [ObservableProperty] private bool _livePreview;
    [ObservableProperty] private bool _previewEcho;
    [ObservableProperty] private bool _previewReverb;
    [ObservableProperty] private bool _previewWiden;

    partial void OnLivePreviewChanged(bool value)
    {
        PushFx();
        if (value) _snackbar.Show("Live-Vorschau", "Effekte sind beim Abspielen hörbar.",
            ControlAppearance.Info, new SymbolIcon(SymbolRegular.Sparkle24), TimeSpan.FromSeconds(2));
    }

    partial void OnPreviewEchoChanged(bool value) => PushFx();
    partial void OnPreviewReverbChanged(bool value) => PushFx();
    partial void OnPreviewWidenChanged(bool value) => PushFx();

    private void PushFx()
    {
        _liveFx.SetParams(PreviewEcho, PreviewReverb, PreviewWiden,
            EchoDelayMs, EchoFeedback, EchoMix, ReverbMix, WidenAmount);
        _liveFx.Enabled = LivePreview;
    }

    /// <summary>Seite angezeigt: Vorschau ggf. aktivieren.</summary>
    public void OnActivatedFx() => PushFx();

    /// <summary>Seite verlassen: Live-FX aus, damit andere Seiten unbeeinflusst sind.</summary>
    public void OnDeactivatedFx() => _liveFx.Enabled = false;

    partial void OnEchoDelayMsChanged(double value) { if (LivePreview) PushFx(); }
    partial void OnEchoFeedbackChanged(double value) { if (LivePreview) PushFx(); }
    partial void OnEchoMixChanged(double value) { if (LivePreview) PushFx(); }
    partial void OnReverbMixChanged(double value) { if (LivePreview) PushFx(); }
    partial void OnWidenAmountChanged(double value) { if (LivePreview) PushFx(); }

    /// <summary>Lädt den Puffer aus dem aktuellen Track (falls noch nicht geschehen).</summary>
    public void EnsureLoaded()
    {
        if (_targetClip is not null) return; // Clip-Bearbeitung: Puffer nicht überschreiben

        var track = _session.CurrentTrack;
        if (track is null)
        {
            _buffer = null;
            UpdateInfo();
            return;
        }

        if (_bufferPath == track.FilePath && _buffer is not null)
            return;

        var (samples, sr) = AudioProcessingHelper.ReadStereo(track.FilePath);
        _buffer = samples;
        _sampleRate = sr;
        _bufferPath = track.FilePath;
        if (!IsTemp(track.FilePath))
            _originalPath = track.FilePath;

        _undo.Clear();
        ClearSelection();
        UpdateInfo();
        NotifyAll();
    }

    public void SetSelection(double a, double b)
    {
        SelectionStart = Math.Clamp(Math.Min(a, b), 0, 1);
        SelectionEnd = Math.Clamp(Math.Max(a, b), 0, 1);
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectionStart = double.NaN;
        SelectionEnd = double.NaN;
    }

    partial void OnSelectionStartChanged(double value) => RefreshSelection();
    partial void OnSelectionEndChanged(double value) => RefreshSelection();

    private void RefreshSelection()
    {
        OnPropertyChanged(nameof(HasSelection));
        if (HasSelection && _buffer is not null && _sampleRate > 0)
        {
            var (a, b) = SelectionFrames();
            SelectionInfo = $"Auswahl: {Time(a)} – {Time(b)}  ({Time(b - a)} lang)";
        }
        else
        {
            SelectionInfo = "Keine Auswahl — auf der Wellenform ziehen, um einen Bereich zu markieren.";
        }
        TrimCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        SilenceCommand.NotifyCanExecuteChanged();
        FadeInCommand.NotifyCanExecuteChanged();
        FadeOutCommand.NotifyCanExecuteChanged();
    }

    private (int, int) SelectionFrames()
    {
        var frames = AudioEdits.FrameCount(_buffer!);
        var a = (int)(SelectionStart * frames);
        var b = (int)(SelectionEnd * frames);
        return (a, b);
    }

    private bool CanEdit => _buffer is not null && HasSelection;

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void Trim()
    {
        var (a, b) = SelectionFrames();
        Apply(AudioEdits.Trim(_buffer!, a, b), "Getrimmt");
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void Delete()
    {
        var (a, b) = SelectionFrames();
        var next = AudioEdits.Delete(_buffer!, a, b);
        if (AudioEdits.FrameCount(next) < 1)
        {
            Notify("Löschen nicht möglich", "Es würde nichts übrig bleiben.", false);
            return;
        }
        Apply(next, "Bereich gelöscht");
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void Silence()
    {
        var (a, b) = SelectionFrames();
        Apply(AudioEdits.Silence(_buffer!, a, b), "Stille eingefügt");
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void FadeIn()
    {
        var (a, b) = SelectionFrames();
        Apply(AudioEdits.Fade(_buffer!, a, b, fadeIn: true), "Fade-In");
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void FadeOut()
    {
        var (a, b) = SelectionFrames();
        Apply(AudioEdits.Fade(_buffer!, a, b, fadeIn: false), "Fade-Out");
    }

    // ---- Effekte (Auswahl, sonst ganzer Track) ----

    [ObservableProperty] private double _echoDelayMs = 300;
    [ObservableProperty] private double _echoFeedback = 0.4;
    [ObservableProperty] private double _echoMix = 0.5;
    [ObservableProperty] private double _reverbMix = 0.3;
    [ObservableProperty] private double _widenAmount = 1.6;

    private bool CanProcess => _buffer is not null;

    private (int, int) EffectRange()
        => HasSelection ? SelectionFrames() : (0, AudioEdits.FrameCount(_buffer!));

    [RelayCommand(CanExecute = nameof(CanProcess))]
    private void Normalize()
    {
        var (a, b) = EffectRange();
        Apply(AudioEffects.Normalize(_buffer!, a, b), "Normalisiert (−1 dBFS)");
    }

    [RelayCommand(CanExecute = nameof(CanProcess))]
    private void Echo()
    {
        var (a, b) = EffectRange();
        Apply(AudioEffects.Echo(_buffer!, a, b, _sampleRate, EchoDelayMs, EchoFeedback, EchoMix), "Echo");
    }

    [RelayCommand(CanExecute = nameof(CanProcess))]
    private void Reverb()
    {
        var (a, b) = EffectRange();
        Apply(AudioEffects.Reverb(_buffer!, a, b, _sampleRate, ReverbMix), "Hall");
    }

    [RelayCommand(CanExecute = nameof(CanProcess))]
    private void Widen()
    {
        var (a, b) = EffectRange();
        Apply(AudioEffects.StereoWiden(_buffer!, a, b, WidenAmount), "Stereo verbreitert");
    }

    [RelayCommand(CanExecute = nameof(CanProcess))]
    private void Reverse()
    {
        var (a, b) = EffectRange();
        Apply(AudioEffects.Reverse(_buffer!, a, b), "Umgekehrt");
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_undo.Count == 0) return;
        _buffer = _undo.Pop();
        Commit();
        Notify("Rückgängig", "Letzte Änderung zurückgenommen.", true);
    }

    private void Apply(float[] next, string message)
    {
        _undo.Push(_buffer!);            // alten Stand sichern
        _buffer = next;
        Commit();
        Notify(message, $"Neue Länge: {Time(AudioEdits.FrameCount(_buffer))}", true);
    }

    private void Commit()
    {
        if (_buffer is null) return;

        var temp = Path.Combine(EditDir, $"edit_{Guid.NewGuid():N}.wav");
        AudioEdits.WriteWav(temp, _buffer, _sampleRate);

        // Im Clip-Modus die Sitzung NICHT überschreiben (Transport folgt sonst dem Studio).
        if (_targetClip is null)
        {
            _session.CurrentTrack = new AudioTrack
            {
                FilePath = temp,
                Duration = TimeSpan.FromSeconds((double)AudioEdits.FrameCount(_buffer) / _sampleRate),
                SampleRate = _sampleRate,
                Channels = AudioEdits.Channels,
                Peaks = AudioEdits.ComputePeaks(_buffer)
            };
        }
        _player.Load(temp);
        _bufferPath = temp;

        // Vorherige temporäre Datei aufräumen (Player hat sie freigegeben).
        if (_previousTemp is not null && IsTemp(_previousTemp) && File.Exists(_previousTemp))
        {
            try { File.Delete(_previousTemp); } catch { /* egal */ }
        }
        _previousTemp = temp;

        ClearSelection();
        UpdateInfo();
        NotifyAll();
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (_buffer is null) return;

        var dialog = new SaveFileDialog
        {
            Title = "Bearbeitetes Audio exportieren",
            Filter = AudioExporter.SaveFilter,
            FileName = (_originalPath is null ? "audio" : Path.GetFileNameWithoutExtension(_originalPath)) + "-edit.wav"
        };
        if (dialog.ShowDialog() != true) return;

        var snapshot = _buffer;
        var sr = _sampleRate;
        try
        {
            await Task.Run(() =>
                AudioExporter.Export(new FloatArraySampleProvider(snapshot, sr, AudioEdits.Channels), dialog.FileName));
            Notify("Exportiert", Path.GetFileName(dialog.FileName), true);
        }
        catch (Exception ex)
        {
            Notify("Export fehlgeschlagen", ex.Message, false);
        }
    }

    private void UpdateInfo()
    {
        if (_buffer is null || _sampleRate == 0)
        {
            FormatInfo = "";
            PathInfo = "";
            DisplayName = "Keine Datei geladen";
            Peaks = null;
            return;
        }
        var dur = TimeSpan.FromSeconds((double)AudioEdits.FrameCount(_buffer) / _sampleRate);
        FormatInfo = $"{_sampleRate} Hz  ·  Stereo  ·  {dur:mm\\:ss}";
        PathInfo = _originalPath ?? "";
        DisplayName = string.IsNullOrEmpty(_originalPath) ? "Bearbeitung" : Path.GetFileName(_originalPath);
        // Eigene Wellenform-Spitzen aus dem Arbeitspuffer — funktioniert auch im Clip-Modus,
        // in dem die Sitzung (Session.CurrentTrack) bewusst nicht überschrieben wird.
        Peaks = AudioEdits.ComputePeaks(_buffer);
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(HasTrack));
        OnPropertyChanged(nameof(CanUndo));
        UndoCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
        NormalizeCommand.NotifyCanExecuteChanged();
        EchoCommand.NotifyCanExecuteChanged();
        ReverbCommand.NotifyCanExecuteChanged();
        WidenCommand.NotifyCanExecuteChanged();
        ReverseCommand.NotifyCanExecuteChanged();
        RefreshSelection();
    }

    private void Notify(string title, string msg, bool ok)
        => _snackbar.Show(title, msg,
            ok ? ControlAppearance.Success : ControlAppearance.Caution,
            new SymbolIcon(ok ? SymbolRegular.CheckmarkCircle24 : SymbolRegular.Warning24),
            TimeSpan.FromSeconds(ok ? 2 : 4));

    private static bool IsTemp(string path) => path.StartsWith(EditDir, StringComparison.OrdinalIgnoreCase);

    private string Time(int frames) => Time(TimeSpan.FromSeconds((double)frames / Math.Max(1, _sampleRate)));
    private static string Time(TimeSpan t) => $"{(int)t.TotalMinutes:00}:{t.Seconds:00}.{t.Milliseconds / 100}";
}
