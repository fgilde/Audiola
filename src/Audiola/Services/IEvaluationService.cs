using Audiola.Models;

namespace Audiola.Services;

public interface IEvaluationService
{
    /// <summary>
    /// Liest eine CSV mit den Spalten Wahrheit (truth/label) und optional
    /// Detektor-Urteil (predicted/verdict) und/oder Score ein.
    /// Nicht interpretierbare Zeilen werden übersprungen.
    /// </summary>
    Task<IReadOnlyList<EvalRow>> ParseAsync(string filePath, CancellationToken ct = default);

    /// <summary>Berechnet Konfusionsmatrix + Kennzahlen. Bei fehlendem Urteil wird der Score am Schwellwert klassifiziert.</summary>
    EvaluationResult Evaluate(IReadOnlyList<EvalRow> rows, double threshold);

    bool HasScores(IReadOnlyList<EvalRow> rows);
}
