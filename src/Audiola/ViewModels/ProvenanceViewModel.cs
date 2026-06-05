using System.Collections.ObjectModel;
using Audiola.Models;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Audiola.ViewModels;

public sealed partial class ProvenanceViewModel : ObservableObject
{
    private readonly IProvenanceService _provenance;

    public SessionState Session { get; }

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = "Track laden oder Datei wählen, dann analysieren.";
    [ObservableProperty] private string _assessment = "";
    [ObservableProperty] private string? _c2paRaw;
    [ObservableProperty] private bool _hasC2paRaw;
    [ObservableProperty] private string _analyzedFile = "";

    public ObservableCollection<Finding> Findings { get; } = [];

    public ProvenanceViewModel(SessionState session, IProvenanceService provenance)
    {
        Session = session;
        _provenance = provenance;
    }

    private bool CanAnalyzeTrack => Session.HasTrack && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanAnalyzeTrack))]
    private Task AnalyzeTrackAsync()
        => RunAnalysis(Session.CurrentTrack!.FilePath);

    [RelayCommand]
    private async Task AnalyzeFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Datei für Provenienz-Analyse wählen",
            Filter = "Audio/Medien|*.wav;*.mp3;*.flac;*.m4a;*.ogg;*.aiff|Alle Dateien|*.*"
        };
        if (dialog.ShowDialog() == true)
            await RunAnalysis(dialog.FileName);
    }

    private async Task RunAnalysis(string filePath)
    {
        IsBusy = true;
        AnalyzeTrackCommand.NotifyCanExecuteChanged();
        Findings.Clear();
        Assessment = "";
        C2paRaw = null;
        HasC2paRaw = false;

        try
        {
            StatusText = "Analysiere …";
            var report = await _provenance.AnalyzeAsync(filePath);

            foreach (var f in report.Findings)
                Findings.Add(f);

            Assessment = report.Assessment;
            C2paRaw = report.C2paRaw;
            HasC2paRaw = !string.IsNullOrWhiteSpace(report.C2paRaw);
            AnalyzedFile = System.IO.Path.GetFileName(filePath);

            var aiCount = report.Findings.Count(x => x.Severity == FindingSeverity.AiIndicator);
            StatusText = aiCount > 0
                ? $"{report.Findings.Count} Befunde, davon {aiCount} KI-/Provenienz-Hinweise."
                : $"{report.Findings.Count} Befunde, keine expliziten KI-Marker.";
        }
        catch (Exception ex)
        {
            StatusText = "Fehler: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
            AnalyzeTrackCommand.NotifyCanExecuteChanged();
        }
    }
}
