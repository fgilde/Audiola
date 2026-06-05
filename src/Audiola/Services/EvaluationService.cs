using System.Globalization;
using System.IO;
using Audiola.Models;

namespace Audiola.Services;

/// <summary>
/// Misst die Zuverlässigkeit eines (externen) KI-Detektors anhand einer gelabelten
/// CSV. Reine Auswertung – ruft selbst keinen Detektor auf.
/// </summary>
public sealed class EvaluationService : IEvaluationService
{
    private static readonly string[] TruthHeaders = ["truth", "true", "label", "actual", "ist", "wahr", "ground", "groundtruth"];
    private static readonly string[] PredHeaders = ["pred", "prediction", "predicted", "verdict", "detected", "erkannt", "result", "ergebnis"];
    private static readonly string[] ScoreHeaders = ["score", "prob", "probability", "confidence", "konfidenz", "wert"];

    public async Task<IReadOnlyList<EvalRow>> ParseAsync(string filePath, CancellationToken ct = default)
    {
        var lines = await File.ReadAllLinesAsync(filePath, ct);
        if (lines.Length < 2)
            throw new InvalidOperationException("CSV enthält keine Datenzeilen (Kopfzeile + mindestens eine Zeile erwartet).");

        var delimiter = lines[0].Count(c => c == ';') > lines[0].Count(c => c == ',') ? ';' : ',';
        var header = SplitLine(lines[0], delimiter);

        var truthIdx = FindColumn(header, TruthHeaders);
        var predIdx = FindColumn(header, PredHeaders);
        var scoreIdx = FindColumn(header, ScoreHeaders);

        if (truthIdx < 0)
            throw new InvalidOperationException("Keine Wahrheits-Spalte gefunden (z. B. 'truth', 'label', 'ist').");
        if (predIdx < 0 && scoreIdx < 0)
            throw new InvalidOperationException("Weder Urteils-Spalte ('predicted'/'verdict') noch Score-Spalte gefunden.");

        var rows = new List<EvalRow>();
        for (var i = 1; i < lines.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var cols = SplitLine(lines[i], delimiter);
            var truth = ParseLabel(Get(cols, truthIdx));
            if (truth is null) continue; // ohne Wahrheit unbrauchbar

            var pred = predIdx >= 0 ? ParseLabel(Get(cols, predIdx)) : null;
            var score = scoreIdx >= 0 ? ParseScore(Get(cols, scoreIdx)) : null;

            rows.Add(new EvalRow(truth.Value, pred, score));
        }

        if (rows.Count == 0)
            throw new InvalidOperationException("Keine interpretierbaren Zeilen gefunden.");

        return rows;
    }

    public bool HasScores(IReadOnlyList<EvalRow> rows) => rows.Any(r => r.Score.HasValue);

    public EvaluationResult Evaluate(IReadOnlyList<EvalRow> rows, double threshold)
    {
        int tp = 0, fp = 0, tn = 0, fn = 0, skipped = 0;

        foreach (var r in rows)
        {
            var pred = r.PredAi ?? (r.Score.HasValue ? r.Score.Value >= threshold : (bool?)null);
            if (pred is null) { skipped++; continue; }

            if (r.TruthAi && pred.Value) tp++;
            else if (!r.TruthAi && pred.Value) fp++;
            else if (!r.TruthAi && !pred.Value) tn++;
            else fn++;
        }

        return new EvaluationResult(tp, fp, tn, fn, skipped);
    }

    private static string[] SplitLine(string line, char delimiter)
        => line.Split(delimiter).Select(s => s.Trim().Trim('"').Trim()).ToArray();

    private static string Get(string[] cols, int idx) => idx >= 0 && idx < cols.Length ? cols[idx] : "";

    private static int FindColumn(string[] header, string[] candidates)
    {
        for (var i = 0; i < header.Length; i++)
        {
            var h = header[i].ToLowerInvariant();
            if (candidates.Any(c => h.Contains(c)))
                return i;
        }
        return -1;
    }

    private static bool? ParseLabel(string s)
    {
        s = s.Trim().ToLowerInvariant();
        return s switch
        {
            "ai" or "ki" or "1" or "true" or "yes" or "ja" or "positive" or "pos" or "fake" or "synthetic" or "generated" => true,
            "human" or "mensch" or "real" or "0" or "false" or "no" or "nein" or "negative" or "neg" or "echt" or "organic" => false,
            _ => null
        };
    }

    private static double? ParseScore(string s)
    {
        s = s.Trim();
        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v;
        if (double.TryParse(s.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out v))
            return v;
        return null;
    }
}
