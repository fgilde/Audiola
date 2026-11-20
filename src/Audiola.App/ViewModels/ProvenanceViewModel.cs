using System.Collections.ObjectModel;
using Audiola.Models;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Audiola.ViewModels;

public sealed partial class ProvenanceViewModel : ObservableObject
{
    private readonly IProvenanceService _provenance;
    private readonly IFileDialogs _files;
    private readonly TimelineViewModel _timeline;

    public SessionState Session { get; }

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = L.T("Track laden, Studio-Mix verwenden oder Datei wählen, dann analysieren.");
    [ObservableProperty] private string _assessment = "";
    [ObservableProperty] private string? _c2paRaw;
    [ObservableProperty] private bool _hasC2paRaw;
    [ObservableProperty] private string _analyzedFile = "";

    public ObservableCollection<Finding> Findings { get; } = [];

    /// <summary>Für den Leerzustand der Seite: erst nach einer Analyse gibt es etwas zu zeigen.</summary>
    public bool HasFindings => Findings.Count > 0;

    public ProvenanceViewModel(SessionState session, IProvenanceService provenance, TimelineViewModel timeline,
        IFileDialogs files)
    {
        Session = session;
        _provenance = provenance;
        _timeline = timeline;
        _files = files;

        // Button-Verfügbarkeit aktualisieren, sobald sich ein geladener Track oder die
        // Studio-Spuren ändern (z. B. nach dem Öffnen eines Audiola-Projekts).
        Session.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SessionState.HasTrack)) AnalyzeTrackCommand.NotifyCanExecuteChanged();
        };
        _timeline.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TimelineViewModel.HasTracks)) AnalyzeTrackCommand.NotifyCanExecuteChanged();
        };
    }

    /// <summary>Analysiert werden kann ein geladener Track oder – sonst – der aktuelle Studio-Mix.</summary>
    private bool CanAnalyzeTrack => (Session.HasTrack || _timeline.HasTracks) && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanAnalyzeTrack))]
    private async Task AnalyzeTrackAsync()
    {
        // Bevorzugt eine konkret geladene Datei; sonst den aktuellen Studio-Mix rendern.
        if (Session.CurrentTrack?.FilePath is { } file)
        {
            await RunAnalysis(file);
            return;
        }

        if (!_timeline.HasTracks) return;

        IsBusy = true;
        AnalyzeTrackCommand.NotifyCanExecuteChanged();
        string? mix;
        try
        {
            StatusText = L.T("Studio-Mix wird gerendert …");
            mix = await _timeline.RenderMixToTempFileAsync();
        }
        catch (Exception ex)
        {
            StatusText = L.F("Fehler beim Rendern des Mix: {0}", ex.Message);
            IsBusy = false;
            AnalyzeTrackCommand.NotifyCanExecuteChanged();
            return;
        }
        IsBusy = false;
        if (mix is not null) await RunAnalysis(mix);
    }

    [RelayCommand]
    private async Task AnalyzeFileAsync()
    {
        var path = await _files.OpenFileAsync(L.T("Datei für Provenienz-Analyse wählen"),
            new FileFilter(L.T("Audio/Medien"), "wav", "mp3", "flac", "m4a", "ogg", "aiff"), FileFilter.Any);
        if (path is not null)
            await RunAnalysis(path);
    }

    private async Task RunAnalysis(string filePath)
    {
        IsBusy = true;
        AnalyzeTrackCommand.NotifyCanExecuteChanged();
        Findings.Clear();
        OnPropertyChanged(nameof(HasFindings));
        Assessment = "";
        C2paRaw = null;
        HasC2paRaw = false;

        try
        {
            StatusText = L.T("Analysiere …");
            var report = await _provenance.AnalyzeAsync(filePath);

            foreach (var f in report.Findings)
                Findings.Add(f);
            OnPropertyChanged(nameof(HasFindings));

            Assessment = report.Assessment;
            C2paRaw = report.C2paRaw;
            HasC2paRaw = !string.IsNullOrWhiteSpace(report.C2paRaw);
            AnalyzedFile = System.IO.Path.GetFileName(filePath);

            var aiCount = report.Findings.Count(x => x.Severity == FindingSeverity.AiIndicator);
            StatusText = aiCount > 0
                ? L.F("{0} Befunde, davon {1} KI-/Provenienz-Hinweise.", report.Findings.Count, aiCount)
                : L.F("{0} Befunde, keine expliziten KI-Marker.", report.Findings.Count);
        }
        catch (Exception ex)
        {
            StatusText = L.F("Fehler: {0}", ex.Message);
        }
        finally
        {
            IsBusy = false;
            AnalyzeTrackCommand.NotifyCanExecuteChanged();
        }
    }
}
