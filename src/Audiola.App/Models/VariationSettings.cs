namespace Audiola.Models;

/// <summary>
/// Subtile, reproduzierbare Klangvariation mit konservativen Parametern.
/// Alle Werte sind normalisiert und fuer songnahe Ergebnisse begrenzt.
/// </summary>
public sealed class VariationSettings
{
    /// <summary>Globale Wirkung der Variation [0..1].</summary>
    public double Intensity { get; set; } = 0.35;

    /// <summary>Negativ = waermer, positiv = heller [-1..1].</summary>
    public double TimbreShift { get; set; } = 0.12;

    /// <summary>Negativ = weicher, positiv = praesenter [-1..1].</summary>
    public double PresenceShift { get; set; } = 0.08;

    /// <summary>Negativ = schmaler, positiv = breiter [-1..1].</summary>
    public double StereoWidth { get; set; } = 0.10;

    /// <summary>Leichte harmonische Verdichtung [0..1].</summary>
    public double Saturation { get; set; } = 0.16;

    /// <summary>Sehr leises Feinkorn / Noise-Floor-Shaping [0..1].</summary>
    public double Texture { get; set; } = 0.10;

    /// <summary>Passt die Ausgangslautheit wieder an die Eingangslautheit an.</summary>
    public bool MatchInputLoudness { get; set; } = true;

    /// <summary>Seed fuer reproduzierbare Varianten.</summary>
    public int Seed { get; set; } = 13579;
}
