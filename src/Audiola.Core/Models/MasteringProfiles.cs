namespace Audiola.Models;

/// <summary>Ein benanntes Mastering-Preset.</summary>
public sealed record MasteringProfile(string Name, string Description, MasteringSettings Settings);

/// <summary>Ein vom Benutzer gespeichertes Profil (persistiert in den Einstellungen).</summary>
public sealed class SavedMasteringProfile
{
    public string Name { get; set; } = "";
    public MasteringSettings Settings { get; set; } = new();
}

/// <summary>
/// Kuratierte Mastering-Presets. Werte sind bewusst moderat gewählt (Glue statt
/// Zerstörung); die LUFS-Ziele orientieren sich an gängigen Plattform-/Genre-Normen
/// (Streaming ≈ -14, lauter Pop/Club ≈ -8…-10, Sprache/Podcast ≈ -16 LUFS).
/// </summary>
public static class MasteringProfiles
{
    public static IReadOnlyList<MasteringProfile> All { get; } =
    [
        new("Neutral (aus)", "Keine Bearbeitung – Bypass.", new MasteringSettings
        {
            HighPassEnabled = false, LowShelfGainDb = 0, MidGainDb = 0, HighShelfGainDb = 0,
            CompressorEnabled = false, NormalizeLoudness = false
        }),

        new("Streaming −14 LUFS", "Sanftes, plattformkonformes Master (Spotify/Apple/YouTube).", new MasteringSettings
        {
            HighPassEnabled = true, HighPassHz = 30,
            LowShelfHz = 100, LowShelfGainDb = 0,
            MidHz = 1000, MidQ = 1.0, MidGainDb = 0,
            HighShelfHz = 10000, HighShelfGainDb = 1,
            CompressorEnabled = true, ThresholdDb = -18, Ratio = 2.0, AttackMs = 15, ReleaseMs = 150, MakeupGainDb = 0,
            NormalizeLoudness = true, TargetLufs = -14
        }),

        new("Pop", "Hell und präsent, kontrollierte Tiefen, modern laut.", new MasteringSettings
        {
            HighPassEnabled = true, HighPassHz = 35,
            LowShelfHz = 100, LowShelfGainDb = 1.5,
            MidHz = 3000, MidQ = 0.9, MidGainDb = 1.5,
            HighShelfHz = 12000, HighShelfGainDb = 3,
            CompressorEnabled = true, ThresholdDb = -20, Ratio = 2.5, AttackMs = 10, ReleaseMs = 120, MakeupGainDb = 0,
            NormalizeLoudness = true, TargetLufs = -10
        }),

        new("Rock", "Mitten-Präsenz und Glue-Kompression für Band-Material.", new MasteringSettings
        {
            HighPassEnabled = true, HighPassHz = 40,
            LowShelfHz = 90, LowShelfGainDb = 1,
            MidHz = 2000, MidQ = 1.0, MidGainDb = 2,
            HighShelfHz = 10000, HighShelfGainDb = 1.5,
            CompressorEnabled = true, ThresholdDb = -18, Ratio = 3.0, AttackMs = 20, ReleaseMs = 180, MakeupGainDb = 0,
            NormalizeLoudness = true, TargetLufs = -10
        }),

        new("Radio-Ready", "Dicht, verständlich, durchsetzungsstark.", new MasteringSettings
        {
            HighPassEnabled = true, HighPassHz = 45,
            LowShelfHz = 110, LowShelfGainDb = 0,
            MidHz = 3500, MidQ = 0.8, MidGainDb = 2,
            HighShelfHz = 12000, HighShelfGainDb = 2,
            CompressorEnabled = true, ThresholdDb = -22, Ratio = 3.5, AttackMs = 5, ReleaseMs = 100, MakeupGainDb = 0,
            NormalizeLoudness = true, TargetLufs = -11
        }),

        new("Loud / Club", "Maximal laut mit Sub-Punch für Clubsysteme.", new MasteringSettings
        {
            HighPassEnabled = true, HighPassHz = 30,
            LowShelfHz = 60, LowShelfGainDb = 2,
            MidHz = 1000, MidQ = 1.0, MidGainDb = 0,
            HighShelfHz = 12000, HighShelfGainDb = 1.5,
            CompressorEnabled = true, ThresholdDb = -24, Ratio = 4.0, AttackMs = 5, ReleaseMs = 80, MakeupGainDb = 0,
            NormalizeLoudness = true, TargetLufs = -8
        }),

        new("Warm / Vintage", "Volle Tiefen, sanft gerollte Höhen, weicher Charakter.", new MasteringSettings
        {
            HighPassEnabled = true, HighPassHz = 30,
            LowShelfHz = 120, LowShelfGainDb = 2.5,
            MidHz = 2500, MidQ = 1.0, MidGainDb = -1,
            HighShelfHz = 10000, HighShelfGainDb = -1.5,
            CompressorEnabled = true, ThresholdDb = -18, Ratio = 2.0, AttackMs = 25, ReleaseMs = 200, MakeupGainDb = 0,
            NormalizeLoudness = true, TargetLufs = -13
        }),

        new("Podcast / Sprache", "Tiefpass-Rumpeln raus, Verständlichkeit hoch, dichte Dynamik.", new MasteringSettings
        {
            HighPassEnabled = true, HighPassHz = 80,
            LowShelfHz = 120, LowShelfGainDb = -1,
            MidHz = 3000, MidQ = 0.9, MidGainDb = 2,
            HighShelfHz = 9000, HighShelfGainDb = 1,
            CompressorEnabled = true, ThresholdDb = -22, Ratio = 4.0, AttackMs = 5, ReleaseMs = 120, MakeupGainDb = 2,
            NormalizeLoudness = true, TargetLufs = -16
        }),

        new("Bass-Boost", "Kräftiges Low-End, leicht Luft oben.", new MasteringSettings
        {
            HighPassEnabled = true, HighPassHz = 25,
            LowShelfHz = 80, LowShelfGainDb = 4,
            MidHz = 1000, MidQ = 1.0, MidGainDb = 0,
            HighShelfHz = 12000, HighShelfGainDb = 1,
            CompressorEnabled = true, ThresholdDb = -18, Ratio = 2.0, AttackMs = 15, ReleaseMs = 150, MakeupGainDb = 0,
            NormalizeLoudness = true, TargetLufs = -12
        }),

        new("HipHop / Trap", "Tiefer Sub-Bass, knackige Transienten, laut.", new MasteringSettings
        {
            HighPassEnabled = true, HighPassHz = 28,
            LowShelfHz = 70, LowShelfGainDb = 3,
            MidHz = 1500, MidQ = 1.0, MidGainDb = -1,
            HighShelfHz = 11000, HighShelfGainDb = 2.5,
            CompressorEnabled = true, ThresholdDb = -20, Ratio = 3.0, AttackMs = 8, ReleaseMs = 90, MakeupGainDb = 0,
            NormalizeLoudness = true, TargetLufs = -9
        }),

        new("EDM / Electronic", "Breit, glänzend, druckvoll für Clubsysteme.", new MasteringSettings
        {
            HighPassEnabled = true, HighPassHz = 30,
            LowShelfHz = 60, LowShelfGainDb = 2,
            MidHz = 2000, MidQ = 0.8, MidGainDb = 1,
            HighShelfHz = 13000, HighShelfGainDb = 3,
            CompressorEnabled = true, ThresholdDb = -22, Ratio = 4.0, AttackMs = 5, ReleaseMs = 70, MakeupGainDb = 0,
            NormalizeLoudness = true, TargetLufs = -8
        }),

        new("Acoustic / Folk", "Natürlich und offen, dezente Dynamik.", new MasteringSettings
        {
            HighPassEnabled = true, HighPassHz = 35,
            LowShelfHz = 110, LowShelfGainDb = 0.5,
            MidHz = 2500, MidQ = 0.9, MidGainDb = 1,
            HighShelfHz = 12000, HighShelfGainDb = 1.5,
            CompressorEnabled = true, ThresholdDb = -16, Ratio = 1.8, AttackMs = 25, ReleaseMs = 200, MakeupGainDb = 0,
            NormalizeLoudness = true, TargetLufs = -16
        }),

        new("Lo-Fi", "Weich, gedämpfte Höhen, gemütlich.", new MasteringSettings
        {
            HighPassEnabled = true, HighPassHz = 40,
            LowShelfHz = 120, LowShelfGainDb = 2,
            MidHz = 1000, MidQ = 1.0, MidGainDb = -1.5,
            HighShelfHz = 8000, HighShelfGainDb = -3,
            CompressorEnabled = true, ThresholdDb = -18, Ratio = 2.5, AttackMs = 20, ReleaseMs = 220, MakeupGainDb = 0,
            NormalizeLoudness = true, TargetLufs = -14
        }),

        new("Vocal-Boost", "Stimme nach vorn, klare Präsenz.", new MasteringSettings
        {
            HighPassEnabled = true, HighPassHz = 50,
            LowShelfHz = 120, LowShelfGainDb = -1,
            MidHz = 3000, MidQ = 0.8, MidGainDb = 3,
            HighShelfHz = 11000, HighShelfGainDb = 2,
            CompressorEnabled = true, ThresholdDb = -20, Ratio = 3.0, AttackMs = 8, ReleaseMs = 120, MakeupGainDb = 1,
            NormalizeLoudness = true, TargetLufs = -13
        }),
    ];
}
