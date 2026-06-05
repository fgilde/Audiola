namespace Audiola.Models;

/// <summary>Eine eingelesene Zeile: Wahrheit, ggf. Detektor-Urteil und/oder Score.</summary>
public sealed record EvalRow(bool TruthAi, bool? PredAi, double? Score);

/// <summary>
/// Auswertung der Detektor-Zuverlässigkeit (Positivklasse = „KI“).
/// </summary>
public sealed record EvaluationResult(
    int TruePositives,
    int FalsePositives,
    int TrueNegatives,
    int FalseNegatives,
    int Skipped)
{
    public int Total => TruePositives + FalsePositives + TrueNegatives + FalseNegatives;

    private static double Ratio(int num, int den) => den == 0 ? double.NaN : (double)num / den;

    public double Accuracy => Ratio(TruePositives + TrueNegatives, Total);
    public double Precision => Ratio(TruePositives, TruePositives + FalsePositives);
    public double Recall => Ratio(TruePositives, TruePositives + FalseNegatives);       // TPR / Sensitivität
    public double Specificity => Ratio(TrueNegatives, TrueNegatives + FalsePositives);   // TNR
    public double FalsePositiveRate => Ratio(FalsePositives, FalsePositives + TrueNegatives);
    public double FalseNegativeRate => Ratio(FalseNegatives, FalseNegatives + TruePositives);

    public double F1
    {
        get
        {
            var p = Precision;
            var r = Recall;
            return (double.IsNaN(p) || double.IsNaN(r) || p + r == 0) ? double.NaN : 2 * p * r / (p + r);
        }
    }
}
