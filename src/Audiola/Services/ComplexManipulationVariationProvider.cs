using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Globalization;
using Audiola.Helper;

namespace Audiola.Services;


public sealed class ComplexManipulationVariationProvider : IAudioVariationProvider
{
    public const string CompleteVariationId = "complete";

    private readonly IReadOnlyList<VariationConfig> catalog;
    private readonly IReadOnlyDictionary<string, VariationConfig> variationsById;
    private readonly IReadOnlyList<AudioVariation> variations;

    public ComplexManipulationVariationProvider()
    {
        catalog = BuildCatalog();
        variationsById = catalog.ToDictionary(config => config.Id, StringComparer.OrdinalIgnoreCase);
        variations = catalog
            .Select(config => new AudioVariation(config.Id, config.Name, config.Description))
            .ToArray();
    }

    public string Name => "Other";

    public IReadOnlyList<AudioVariation> GetVariations() => variations;

    public async Task<float[]> ApplyAsync(string variationId, float[] interleavedStereo, int sampleRate, CancellationToken ct = default)
    {
        var result = await ApplyDetailedAsync(variationId, interleavedStereo, sampleRate, ct);
        return result.Samples;
    }

    internal Task<VariationApplicationResult> ApplyDetailedAsync(string variationId, float[] interleavedStereo, int sampleRate, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(variationId))
        {
            throw new ArgumentException("variationId darf nicht leer sein.", nameof(variationId));
        }

        if (interleavedStereo is null)
        {
            throw new ArgumentNullException(nameof(interleavedStereo));
        }

        if (interleavedStereo.Length % 2 != 0)
        {
            throw new ArgumentException("Der Audio-Puffer muss interleaved Stereo enthalten.", nameof(interleavedStereo));
        }

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "sampleRate muss positiv sein.");
        }

        if (!variationsById.TryGetValue(variationId, out var config))
        {
            throw new KeyNotFoundException($"Unbekannte Variation: {variationId}");
        }

        return Task.Run(() => ApplyInternal(config, interleavedStereo, sampleRate, ct), ct);
    }

    private static VariationApplicationResult ApplyInternal(VariationConfig config, float[] interleavedStereo, int sampleRate, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        ISampleProvider provider = new StereoBufferSampleProvider(interleavedStereo, sampleRate);
        var steps = new List<string>();
        var notes = new List<string>(config.ExtraNotes);

        if (config.LowCut > 0 || config.HighCut > 0)
        {
            provider = new BiQuadFilterProvider(provider, config.LowCut, config.HighCut);
            steps.Add($"Filter(lowCut={config.LowCut}, highCut={config.HighCut})");
        }

        if (Math.Abs(config.LowShelfGainDb) > 0.01f || Math.Abs(config.HighShelfGainDb) > 0.01f)
        {
            provider = new ToneShaperProvider(provider, config.LowShelfGainDb, config.HighShelfGainDb);
            steps.Add($"Tone(lowShelfDb={config.LowShelfGainDb.ToString("0.0", CultureInfo.InvariantCulture)}, highShelfDb={config.HighShelfGainDb.ToString("0.0", CultureInfo.InvariantCulture)})");
        }

        if (config.NoiseLevel > 0)
        {
            provider = new NoiseAdder(provider, config.NoiseLevel);
            steps.Add($"Noise(level={config.NoiseLevel.ToString("0.0000", CultureInfo.InvariantCulture)})");
        }

        if (Math.Abs(config.PitchShift) > 0.01 || Math.Abs(config.TimeStretch - 1.0) > 0.001)
        {
            provider = new SoundTouchProfileProvider(provider, config.PitchShift, config.TimeStretch);

            if (Math.Abs(config.PitchShift) > 0.01)
            {
                steps.Add($"PitchShift(semitones={config.PitchShift.ToString("0.0", CultureInfo.InvariantCulture)})");
            }

            if (Math.Abs(config.TimeStretch - 1.0) > 0.001)
            {
                steps.Add($"TimeStretch(factor={config.TimeStretch.ToString("0.000", CultureInfo.InvariantCulture)})");
            }
        }

        if (config.TargetSampleRate > 0 && config.TargetSampleRate != sampleRate)
        {
            provider = new WdlResamplingSampleProvider(provider, config.TargetSampleRate);
            provider = new WdlResamplingSampleProvider(provider, sampleRate);
            steps.Add($"Resample(targetHz={config.TargetSampleRate}, restoreHz={sampleRate})");
            notes.Add("Sample-Rate-Variationen werden fuer dieses Provider-Interface auf die Eingangs-Sample-Rate zurueckgebracht.");
        }

        if (config.Compression)
        {
            provider = new SimpleCompressor(provider);
            steps.Add("Compression");
        }

        if (config.Saturation > 0)
        {
            provider = new SaturationProvider(provider, config.Saturation);
            steps.Add($"Saturation(amount={config.Saturation.ToString("0.00", CultureInfo.InvariantCulture)})");
        }

        if (config.Reverb > 0)
        {
            provider = new SimpleReverbProvider(provider, config.Reverb);
            steps.Add($"Reverb(wet={config.Reverb.ToString("0.00", CultureInfo.InvariantCulture)})");
        }

        if (config.StereoWidth > 0 && Math.Abs(config.StereoWidth - 1.0f) > 0.001f)
        {
            provider = new StereoWidenerProvider(provider, config.StereoWidth);
            steps.Add($"StereoWidth(width={config.StereoWidth.ToString("0.00", CultureInfo.InvariantCulture)})");
        }

        if (config.PhaseFlipRightChannel)
        {
            provider = new PhaseFlipProvider(provider);
            steps.Add("PhaseFlip(rightChannel)");
        }

        if (config.BitDepth is > 0 and < 16)
        {
            provider = new BitCrusherProvider(provider, config.BitDepth);
            steps.Add($"BitCrusher(bitDepth={config.BitDepth})");
        }

        if (config.TremoloDepth > 0 && config.TremoloFrequencyHz > 0)
        {
            provider = new TremoloProvider(provider, config.TremoloDepth, config.TremoloFrequencyHz);
            steps.Add($"Tremolo(depth={config.TremoloDepth.ToString("0.00", CultureInfo.InvariantCulture)}, hz={config.TremoloFrequencyHz.ToString("0.0", CultureInfo.InvariantCulture)})");
        }

        if (Math.Abs(config.OutputGainDb) > 0.01f)
        {
            provider = new GainProvider(provider, config.OutputGainDb);
            steps.Add($"OutputGain(db={config.OutputGainDb.ToString("0.0", CultureInfo.InvariantCulture)})");
        }

        if (config.LimiterThreshold > 0)
        {
            provider = new SoftLimiterProvider(provider, config.LimiterThreshold);
            steps.Add($"Limiter(threshold={config.LimiterThreshold.ToString("0.000", CultureInfo.InvariantCulture)})");
        }

        var processed = SampleProviderRenderer.RenderAllSamples(provider, ct);

        return new VariationApplicationResult(
            processed,
            steps.Count > 0 ? steps : ["Original"],
            config.ToParameterSummary(),
            notes);
    }

    private static IReadOnlyList<VariationConfig> BuildCatalog()
    {
        var variations = new List<VariationConfig>
        {
            new("original", "Original", "Unveraenderte Referenzdatei.")
        };

        for (var i = 1; i <= 5; i++)
        {
            variations.Add(new VariationConfig(
                $"noise{i}",
                $"Noise{i}",
                $"Fuegt leichtes breitbandiges Rauschen in Stufe {i} hinzu.",
                noiseLevel: i * 0.0035f));
        }

        double[] pitches = { -3, -1.5, 1.5, 3 };
        foreach (var pitch in pitches)
        {
            var id = pitch >= 0
                ? $"pitch_plus_{pitch.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', '_')}"
                : $"pitch_minus_{Math.Abs(pitch).ToString("0.0", CultureInfo.InvariantCulture).Replace('.', '_')}";

            variations.Add(new VariationConfig(
                id,
                $"Pitch{pitch:+0.0;-0.0}",
                $"Verschiebt die Tonhoehe mit SoundTouch um {pitch:+0.0;-0.0} Halbtone.",
                pitchShift: pitch));
        }

        variations.Add(new VariationConfig("time_stretch_104", "TimeStretch104", "Verlangsamt das Material leicht mit Faktor 1.04.", timeStretch: 1.04));
        variations.Add(new VariationConfig("time_stretch_096", "TimeStretch096", "Beschleunigt das Material leicht mit Faktor 0.96.", timeStretch: 0.96));

        variations.Add(new VariationConfig("low_cut_80", "LowCut80", "Entfernt sehr tiefe Frequenzen ueber einen High-Pass bei 80 Hz.", lowCut: 80));
        variations.Add(new VariationConfig("high_cut_16k", "HighCut16k", "Begrenzt die Hoehen ueber einen Low-Pass bei 16 kHz.", highCut: 16000));
        variations.Add(new VariationConfig("band_cut_mids", "BandCutMids", "Kombiniert Hoch- und Tiefpass fuer eine deutliche Mittenverlagerung.", lowCut: 250, highCut: 4000));
        variations.Add(new VariationConfig("resample_22k", "Resample22k", "Resampelt herunter und wieder zurueck, um Codec-/Bandbreitenartefakte zu simulieren.", targetSampleRate: 22050));

        variations.Add(new VariationConfig("compression", "Compression", "Komprimiert Pegelspitzen dezent.", compression: true));
        variations.Add(new VariationConfig("saturation", "Saturation", "Fuegt weiche harmonische Saettigung hinzu.", saturation: 0.2f));
        variations.Add(new VariationConfig("light_reverb", "LightReverb", "Mischt einen kurzen, einfachen Raumanteil bei.", reverb: 0.18f));
        variations.Add(new VariationConfig("bit_crusher_10", "BitCrusher10", "Reduziert die Aufloesung auf 10 Bit.", bitDepth: 10));
        variations.Add(new VariationConfig("tremolo_5hz", "Tremolo5Hz", "Moduliert die Lautstaerke mit 5 Hz.", tremoloDepth: 0.35f, tremoloFrequencyHz: 5.0));

        variations.Add(new VariationConfig("stereo_wide_130", "StereoWide130", "Erweitert die Stereobreite moderat.", stereoWidth: 1.3f));
        variations.Add(new VariationConfig("stereo_wide_160", "StereoWide160", "Erweitert die Stereobreite deutlich.", stereoWidth: 1.6f));
        variations.Add(new VariationConfig("phase_flip_right", "PhaseFlipRight", "Invertiert den rechten Kanal phasenmaessig.", phaseFlipRightChannel: true));

        variations.Add(new VariationConfig("gentle_tilt_bright", "GentleTiltBright", "Leichter Tilt-EQ Richtung heller Klang mit Sicherheits-Limiter.", highShelfGainDb: 1.2f, lowShelfGainDb: -0.6f, outputGainDb: -0.8f, limiterThreshold: 0.97f));
        variations.Add(new VariationConfig("gentle_tilt_warm", "GentleTiltWarm", "Leichter Tilt-EQ Richtung waermerer Klang mit Sicherheits-Limiter.", highShelfGainDb: -0.9f, lowShelfGainDb: 0.8f, outputGainDb: -0.8f, limiterThreshold: 0.97f));
        variations.Add(new VariationConfig("safe_limiter", "SafeLimiter", "Reduziert den Ausgangspegel und begrenzt Spitzen sanft.", outputGainDb: -1.2f, limiterThreshold: 0.96f));

        variations.Add(new VariationConfig("strong_combo1", "StrongCombo1", "Kombiniert Rauschen, Pitch, Filter, Resampling und Stereo-Breite.", noiseLevel: 0.008f, pitchShift: 1.2, targetSampleRate: 32000, lowCut: 80, highCut: 16000, saturation: 0.15f, stereoWidth: 1.25f));
        variations.Add(new VariationConfig("strong_combo2", "StrongCombo2", "Kombiniert Rauschen, Time-Stretch, Filter, Kompression, Reverb und Tremolo.", noiseLevel: 0.012f, timeStretch: 1.04, lowCut: 100, highCut: 14000, compression: true, reverb: 0.15f, tremoloDepth: 0.20f, tremoloFrequencyHz: 4.5));
        variations.Add(new VariationConfig("aggressive", "Aggressive", "Setzt mehrere auffaellige Eingriffe fuer einen starken Kontrast ein.", noiseLevel: 0.015f, pitchShift: -1.8, targetSampleRate: 24000, lowCut: 120, saturation: 0.25f, reverb: 0.25f, bitDepth: 10, phaseFlipRightChannel: true));

        variations.Add(new VariationConfig(
            CompleteVariationId,
            "DemoComplete",
            "Klangschonende Komplettkette fuer den Gesamtvergleich.",
            lowCut: 32,
            highCut: 17500,
            compression: true,
            saturation: 0.035f,
            reverb: 0.035f,
            stereoWidth: 1.08f,
            lowShelfGainDb: -0.4f,
            highShelfGainDb: 0.7f,
            outputGainDb: -1.4f,
            limiterThreshold: 0.965f,
            extraNotes:
            [
                "Dieses File nutzt bewusst nur die klangschonenden Techniken in einer sehr dezenten Kette.",
                "Destruktivere Effekte wie BitCrusher, PhaseFlip und Tremolo bleiben als Einzelvarianten fuer Vergleichstests getrennt."
            ]));

        return variations;
    }
}

internal sealed record VariationApplicationResult(
    float[] Samples,
    IReadOnlyList<string> AppliedSteps,
    string ParameterSummary,
    IReadOnlyList<string> Notes);

internal sealed class VariationConfig
{
    public VariationConfig(
        string id,
        string name,
        string description,
        float noiseLevel = 0,
        double pitchShift = 0,
        double timeStretch = 1.0,
        int targetSampleRate = 0,
        int lowCut = 0,
        int highCut = 0,
        bool compression = false,
        float saturation = 0,
        float reverb = 0,
        float stereoWidth = 1.0f,
        bool phaseFlipRightChannel = false,
        int bitDepth = 0,
        float tremoloDepth = 0,
        double tremoloFrequencyHz = 0,
        float lowShelfGainDb = 0,
        float highShelfGainDb = 0,
        float outputGainDb = 0,
        float limiterThreshold = 0,
        IEnumerable<string>? extraNotes = null)
    {
        Id = id;
        Name = name;
        Description = description;
        NoiseLevel = noiseLevel;
        PitchShift = pitchShift;
        TimeStretch = timeStretch;
        TargetSampleRate = targetSampleRate;
        LowCut = lowCut;
        HighCut = highCut;
        Compression = compression;
        Saturation = saturation;
        Reverb = reverb;
        StereoWidth = stereoWidth;
        PhaseFlipRightChannel = phaseFlipRightChannel;
        BitDepth = bitDepth;
        TremoloDepth = tremoloDepth;
        TremoloFrequencyHz = tremoloFrequencyHz;
        LowShelfGainDb = lowShelfGainDb;
        HighShelfGainDb = highShelfGainDb;
        OutputGainDb = outputGainDb;
        LimiterThreshold = limiterThreshold;
        ExtraNotes = extraNotes?.ToArray() ?? [];
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public float NoiseLevel { get; }
    public double PitchShift { get; }
    public double TimeStretch { get; }
    public int TargetSampleRate { get; }
    public int LowCut { get; }
    public int HighCut { get; }
    public bool Compression { get; }
    public float Saturation { get; }
    public float Reverb { get; }
    public float StereoWidth { get; }
    public bool PhaseFlipRightChannel { get; }
    public int BitDepth { get; }
    public float TremoloDepth { get; }
    public double TremoloFrequencyHz { get; }
    public float LowShelfGainDb { get; }
    public float HighShelfGainDb { get; }
    public float OutputGainDb { get; }
    public float LimiterThreshold { get; }
    public IReadOnlyList<string> ExtraNotes { get; }

    public string ToParameterSummary()
    {
        var values = new List<string>
        {
            $"noise={NoiseLevel.ToString("0.0000", CultureInfo.InvariantCulture)}",
            $"pitch={PitchShift.ToString("0.0", CultureInfo.InvariantCulture)}",
            $"timeStretch={TimeStretch.ToString("0.000", CultureInfo.InvariantCulture)}",
            $"sampleRate={TargetSampleRate}",
            $"lowCut={LowCut}",
            $"highCut={HighCut}",
            $"compression={(Compression ? "on" : "off")}",
            $"saturation={Saturation.ToString("0.00", CultureInfo.InvariantCulture)}",
            $"reverb={Reverb.ToString("0.00", CultureInfo.InvariantCulture)}",
            $"stereoWidth={StereoWidth.ToString("0.00", CultureInfo.InvariantCulture)}",
            $"phaseFlipRight={(PhaseFlipRightChannel ? "on" : "off")}",
            $"bitDepth={BitDepth}",
            $"tremoloDepth={TremoloDepth.ToString("0.00", CultureInfo.InvariantCulture)}",
            $"tremoloHz={TremoloFrequencyHz.ToString("0.0", CultureInfo.InvariantCulture)}",
            $"lowShelfDb={LowShelfGainDb.ToString("0.0", CultureInfo.InvariantCulture)}",
            $"highShelfDb={HighShelfGainDb.ToString("0.0", CultureInfo.InvariantCulture)}",
            $"outputGainDb={OutputGainDb.ToString("0.0", CultureInfo.InvariantCulture)}",
            $"limiterThreshold={LimiterThreshold.ToString("0.000", CultureInfo.InvariantCulture)}"
        };

        return string.Join(", ", values);
    }
}
