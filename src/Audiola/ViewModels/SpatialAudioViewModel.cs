using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using Audiola.Models;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NAudio.Wave;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Audiola.ViewModels;

/// <summary>
/// „Spatial Audio": platziert die aktiven Studio-Spuren im 3D-Raum und rendert
/// daraus einen binauralen Kopfhörer-Mix oder eine Mehrkanal-WAV (5.1 / 7.1 / 7.1.4
/// Atmos-Bett). Kein lizenziertes Atmos-Bitstream — der räumliche Effekt entsteht
/// per HRTF-Näherung bzw. VBAP-Verteilung in <see cref="SpatialAudioService"/>.
/// </summary>
public sealed partial class SpatialAudioViewModel : ObservableObject
{
    private readonly TimelineViewModel _timeline;
    private readonly ISnackbarService _snackbar;
    private readonly SpatialPreviewEngine _preview;

    /// <summary>Beim Projekt-Laden gesetzt; wird beim nächsten Aufbau der Quellen übernommen.</summary>
    private ProjectSpatialDto? _pendingSpatial;

    public ObservableCollection<SpatialSourceViewModel> Sources { get; } = [];

    public IReadOnlyList<string> Layouts { get; } = ["5.1 (Surround)", "7.1 (Surround)", "7.1.4 (Atmos-Bett)"];

    [ObservableProperty] private string _selectedLayout = "7.1.4 (Atmos-Bett)";
    [ObservableProperty] private double _roomAmount = 0.18;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isPreviewing;
    [ObservableProperty] private string _statusText = "Quelle wird vorbereitet …";

    public SpatialAudioViewModel(TimelineViewModel timeline, ISnackbarService snackbar, SpatialPreviewEngine preview)
    {
        _timeline = timeline;
        _snackbar = snackbar;
        _preview = preview;
        _preview.Stopped += (_, _) => System.Windows.Application.Current?.Dispatcher.Invoke(() => IsPreviewing = false);
    }

    private SpatialLayout SelectedLayoutEnum =>
        SelectedLayout.StartsWith("5.1") ? SpatialLayout.Surround51 :
        SelectedLayout.StartsWith("7.1.4") ? SpatialLayout.Atmos714 :
        SpatialLayout.Surround71;

    private string LayoutTag => SelectedLayoutEnum switch
    {
        SpatialLayout.Surround51 => "5_1",
        SpatialLayout.Surround71 => "7_1",
        _ => "7_1_4"
    };

    /// <summary>
    /// Aktive Studio-Spuren übernehmen. Reihenfolge: geladene Projekt-Werte (einmalig) →
    /// vorhandene Positionen (beim erneuten Öffnen erhalten) → sinnvolle Standardwerte.
    /// </summary>
    public void PrepareFromStudio()
    {
        StopPreview();
        var prev = new Dictionary<string, SpatialSourceViewModel>();
        foreach (var s in Sources) prev[s.FilePath] = s;

        var built = new List<SpatialSourceViewModel>();
        var idx = 0;
        foreach (var t in _timeline.Tracks)
        {
            var path = t.Model.FilePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;

            var src = new SpatialSourceViewModel(path, t.DisplayName, t.AccentColor);
            if (_pendingSpatial is { } ps && idx < ps.Sources.Count)
            {
                var d = ps.Sources[idx];
                src.AzimuthDeg = d.AzimuthDeg; src.ElevationDeg = d.ElevationDeg;
                src.Distance = d.Distance; src.GainDb = d.GainDb; src.Muted = d.Muted;
            }
            else if (prev.TryGetValue(path, out var existing))
            {
                src.AzimuthDeg = existing.AzimuthDeg; src.ElevationDeg = existing.ElevationDeg;
                src.Distance = existing.Distance; src.GainDb = existing.GainDb; src.Muted = existing.Muted;
            }
            else
            {
                var (az, el) = DefaultPlacement(t.Name, idx);
                src.AzimuthDeg = az; src.ElevationDeg = el;
            }
            built.Add(src);
            idx++;
        }

        Sources.Clear();
        foreach (var s in built) Sources.Add(s);
        _pendingSpatial = null;

        StatusText = Sources.Count == 0
            ? "Keine Spuren — im Studio Stems laden oder trennen."
            : $"{Sources.Count} Spuren bereit — Positionen ziehen, dann Vorschau/Export.";
        UpdateCommands();
    }

    /// <summary>Aktuellen Spatial-Zustand für die Projektdatei serialisieren (null = nichts eingerichtet).</summary>
    public ProjectSpatialDto? ExportSpatial()
    {
        if (Sources.Count == 0) return null;
        return new ProjectSpatialDto
        {
            Layout = SelectedLayout,
            RoomAmount = RoomAmount,
            Sources = Sources.Select(s => new ProjectSpatialSourceDto
            {
                Name = s.Name,
                AzimuthDeg = s.AzimuthDeg,
                ElevationDeg = s.ElevationDeg,
                Distance = s.Distance,
                GainDb = s.GainDb,
                Muted = s.Muted
            }).ToList()
        };
    }

    /// <summary>Spatial-Zustand aus einem Projekt übernehmen (wird beim nächsten Aufbau angewandt).</summary>
    public void ImportSpatial(ProjectSpatialDto? dto)
    {
        _pendingSpatial = dto;
        if (dto is null) return;
        if (Layouts.Contains(dto.Layout)) SelectedLayout = dto.Layout;
        RoomAmount = dto.RoomAmount;
        // Falls die Seite bereits Quellen zeigt, sofort anwenden.
        if (Sources.Count > 0) PrepareFromStudio();
    }

    public void OnDeactivated() => StopPreview();

    private static (double Az, double El) DefaultPlacement(string name, int i) => name.ToLowerInvariant() switch
    {
        "vocals" => (0, 0),
        "drums" => (0, 30),
        "bass" => (0, 0),
        "guitar" => (-45, 10),
        "piano" => (45, 10),
        _ => (i % 2 == 0 ? -60 : 60, 15)
    };

    private List<SpatialSource> BuildSources() => Sources
        .Select(v => new SpatialSource(v.FilePath, v.AzimuthDeg, v.ElevationDeg, v.Distance, v.GainDb, v.Muted))
        .ToList();

    private bool CanRun => Sources.Count > 0 && !IsBusy;

    private void UpdateCommands()
    {
        PreviewCommand.NotifyCanExecuteChanged();
        AutoArrangeCommand.NotifyCanExecuteChanged();
        ExportBinauralCommand.NotifyCanExecuteChanged();
        ExportMultichannelCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void ResetPositions() => PrepareFromStudio();

    /// <summary>Ordnet die Spuren automatisch zu einer plausiblen Bühne an (Lead vorne, Drums leicht erhöht, Instrumente verteilt).</summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private void AutoArrange()
    {
        int guitar = 0, other = 0;
        foreach (var v in Sources)
        {
            (double Az, double El, double Dist) p = v.Name.ToLowerInvariant() switch
            {
                "vocals" => (0, 0, 0.85),
                "bass" => (0, 0, 1.1),
                "drums" => (0, 22, 1.0),
                "piano" => (35, 8, 1.0),
                "guitar" => (guitar++ % 2 == 0 ? -50 : 50, 8, 1.0),
                _ => (other++ % 2 == 0 ? -110 : 110, 18, 1.2)
            };
            v.AzimuthDeg = p.Az; v.ElevationDeg = p.El; v.Distance = p.Dist; v.GainDb = 0;
        }
        StatusText = "Automatisch angeordnet — Lead vorne, Drums leicht erhöht, Instrumente im Raum verteilt.";
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task PreviewAsync()
    {
        IsBusy = true; UpdateCommands();
        try
        {
            StatusText = "Bereite Live-Vorschau vor …";
            await _preview.StartAsync(Sources);
            IsPreviewing = _preview.IsPlaying;
            StatusText = IsPreviewing
                ? "Live-Vorschau läuft (Kopfhörer empfohlen) — Regler wirken sofort."
                : "Keine abspielbaren Spuren.";
        }
        catch (Exception ex) { StatusText = "Fehler: " + ex.Message; }
        finally { IsBusy = false; UpdateCommands(); }
    }

    [RelayCommand]
    private void StopPreview()
    {
        _preview.Stop();
        IsPreviewing = false;
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ExportBinauralAsync()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Binauralen 3D-Mix exportieren",
            Filter = AudioExporter.SaveFilter,
            FileName = "spatial-binaural.wav"
        };
        if (dlg.ShowDialog() != true) return;

        StopPreview();
        IsBusy = true; UpdateCommands();
        try
        {
            StatusText = "Rendere binauralen Mix …";
            var list = BuildSources();
            var room = RoomAmount;
            await Task.Run(() =>
            {
                var (inter, sr) = SpatialAudioService.RenderBinaural(list, room);
                AudioExporter.Export(new FloatArraySampleProvider(inter, sr, 2), dlg.FileName);
            });
            StatusText = "Fertig: " + Path.GetFileName(dlg.FileName);
            _snackbar.Show("Binaural exportiert", Path.GetFileName(dlg.FileName),
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            StatusText = "Fehler: " + ex.Message;
            _snackbar.Show("Export fehlgeschlagen", ex.Message,
                ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(5));
        }
        finally { IsBusy = false; UpdateCommands(); }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ExportMultichannelAsync()
    {
        var layout = SelectedLayoutEnum;
        var dlg = new SaveFileDialog
        {
            Title = "Mehrkanal-WAV exportieren",
            Filter = "WAV (*.wav)|*.wav",
            FileName = $"spatial-{LayoutTag}.wav"
        };
        if (dlg.ShowDialog() != true) return;

        StopPreview();
        IsBusy = true; UpdateCommands();
        try
        {
            StatusText = $"Rendere {SpatialAudioService.ChannelLabel(layout)} …";
            var list = BuildSources();
            await Task.Run(() =>
            {
                var (inter, ch, sr) = SpatialAudioService.RenderMultichannel(list, layout);
                SpatialAudioService.WriteSurroundWav(dlg.FileName, inter, ch, sr, SpatialAudioService.ChannelMask(layout));
            });
            StatusText = "Fertig: " + Path.GetFileName(dlg.FileName);
            _snackbar.Show("Mehrkanal exportiert", Path.GetFileName(dlg.FileName),
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            StatusText = "Fehler: " + ex.Message;
            _snackbar.Show("Export fehlgeschlagen", ex.Message,
                ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(5));
        }
        finally { IsBusy = false; UpdateCommands(); }
    }
}

/// <summary>Eine im Raum platzierte Studio-Spur (mit Live-Position für die Radar-Anzeige).</summary>
public sealed partial class SpatialSourceViewModel : ObservableObject
{
    public string FilePath { get; }
    public string Name { get; }
    public string AccentColor { get; }
    public Brush AccentBrush { get; }

    public SpatialSourceViewModel(string filePath, string name, string accentColor)
    {
        FilePath = filePath;
        Name = name;
        AccentColor = accentColor;
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColor));
        brush.Freeze();
        AccentBrush = brush;
    }

    [ObservableProperty] private double _azimuthDeg;
    [ObservableProperty] private double _elevationDeg;
    [ObservableProperty] private double _distance = 1.0;
    [ObservableProperty] private double _gainDb;
    [ObservableProperty] private bool _muted;

    // ---- Radar (Canvas 220×220, Hörer in der Mitte; 0° = vorn/oben) ----
    private const double Center = 110, DotHalf = 8;

    public double RadarX
    {
        get
        {
            var r = Math.Clamp(Distance, 0.2, 2.0) * 45;
            return Center + Math.Sin(AzimuthDeg * Math.PI / 180.0) * r - DotHalf;
        }
    }

    public double RadarY
    {
        get
        {
            var r = Math.Clamp(Distance, 0.2, 2.0) * 45;
            return Center - Math.Cos(AzimuthDeg * Math.PI / 180.0) * r - DotHalf;
        }
    }

    // Höhere Elevation → größerer Punkt (4..22 px Durchmesser-Zuschlag).
    public double DotSize => 12 + Math.Clamp(ElevationDeg, 0, 90) / 90.0 * 12;

    partial void OnAzimuthDegChanged(double value) => RaiseRadar();
    partial void OnDistanceChanged(double value) => RaiseRadar();
    partial void OnElevationDegChanged(double value) => OnPropertyChanged(nameof(DotSize));

    private void RaiseRadar()
    {
        OnPropertyChanged(nameof(RadarX));
        OnPropertyChanged(nameof(RadarY));
    }
}
