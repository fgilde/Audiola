namespace Audiola.Models;

/// <summary>Parameter der Mastering-Kette: EQ → Kompressor → Makeup → LUFS-Normalisierung.</summary>
public sealed class MasteringSettings
{
    // --- EQ ---
    public bool HighPassEnabled { get; set; } = true;
    public double HighPassHz { get; set; } = 30;

    public double LowShelfHz { get; set; } = 120;
    public double LowShelfGainDb { get; set; } = 0;

    public double MidHz { get; set; } = 1000;
    public double MidQ { get; set; } = 1.0;
    public double MidGainDb { get; set; } = 0;

    public double HighShelfHz { get; set; } = 10000;
    public double HighShelfGainDb { get; set; } = 0;

    // --- Kompressor ---
    public bool CompressorEnabled { get; set; } = true;
    public double ThresholdDb { get; set; } = -18;
    public double Ratio { get; set; } = 2.0;
    public double AttackMs { get; set; } = 10;
    public double ReleaseMs { get; set; } = 150;
    public double MakeupGainDb { get; set; } = 0;

    // --- Loudness ---
    public bool NormalizeLoudness { get; set; } = true;
    public double TargetLufs { get; set; } = -14; // Streaming-typisch
}
