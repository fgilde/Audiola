using Audiola.Models;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Audiola.ViewModels;

/// <summary>
/// Mess-Harness: bewertet die Zuverlässigkeit eines KI-Detektors anhand einer
/// gelabelten CSV (Wahrheit vs. Urteil/Score). Konfusionsmatrix + Kennzahlen.
/// </summary>
public sealed partial class EvaluationViewModel : ObservableObject
{
    private readonly IEvaluationService _eval;
    private IReadOnlyList<EvalRow> _rows = [];

    [ObservableProperty] private string _statusText =
        "CSV laden mit Spalten: truth/label (KI vs. Mensch) und predicted/verdict oder score.";
    [ObservableProperty] private bool _hasResult;
    [ObservableProperty] private bool _hasScores;
    [ObservableProperty] private double _threshold = 0.5;
    [ObservableProperty] private string _fileName = "";

    // Konfusionsmatrix
    [ObservableProperty] private int _truePositives;
    [ObservableProperty] private int _falsePositives;
    [ObservableProperty] private int _trueNegatives;
    [ObservableProperty] private int _falseNegatives;
    [ObservableProperty] private int _total;
    [ObservableProperty] private int _skipped;

    // Kennzahlen (formatiert)
    [ObservableProperty] private string _accuracy = "–";
    [ObservableProperty] private string _precision = "–";
    [ObservableProperty] private string _recall = "–";
    [ObservableProperty] private string _specificity = "–";
    [ObservableProperty] private string _falsePositiveRate = "–";
    [ObservableProperty] private string _falseNegativeRate = "–";
    [ObservableProperty] private string _f1 = "–";

    public EvaluationViewModel(IEvaluationService eval)
    {
        _eval = eval;
    }

    [RelayCommand]
    private async Task LoadCsvAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Detektor-Ergebnisse (CSV) laden",
            Filter = "CSV-Datei|*.csv;*.tsv;*.txt|Alle Dateien|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            _rows = await _eval.ParseAsync(dialog.FileName);
            HasScores = _eval.HasScores(_rows);
            FileName = System.IO.Path.GetFileName(dialog.FileName);
            Recompute();
            StatusText = $"{_rows.Count} Zeilen eingelesen aus {FileName}" +
                         (HasScores ? " — Score-Spalte erkannt, Schwellwert verschiebbar." : "");
        }
        catch (Exception ex)
        {
            HasResult = false;
            StatusText = "Fehler: " + ex.Message;
        }
    }

    partial void OnThresholdChanged(double value)
    {
        if (HasScores && _rows.Count > 0)
            Recompute();
    }

    private void Recompute()
    {
        var r = _eval.Evaluate(_rows, Threshold);

        TruePositives = r.TruePositives;
        FalsePositives = r.FalsePositives;
        TrueNegatives = r.TrueNegatives;
        FalseNegatives = r.FalseNegatives;
        Total = r.Total;
        Skipped = r.Skipped;

        Accuracy = Pct(r.Accuracy);
        Precision = Pct(r.Precision);
        Recall = Pct(r.Recall);
        Specificity = Pct(r.Specificity);
        FalsePositiveRate = Pct(r.FalsePositiveRate);
        FalseNegativeRate = Pct(r.FalseNegativeRate);
        F1 = double.IsNaN(r.F1) ? "–" : r.F1.ToString("F3");

        HasResult = true;
    }

    private static string Pct(double v) => double.IsNaN(v) ? "–" : $"{v * 100:F1} %";
}
