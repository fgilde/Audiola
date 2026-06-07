namespace Audiola.Services;

public sealed class AudioHumanizationProvider : IAudioVariationProvider
{
    public string Name => "Audio Humanization";

    public IReadOnlyList<AudioVariation> GetVariations() =>
    [
        new("H01", "Micro Wow/Flutter", "Sehr kleine tape-artige Timing-Schwankungen."),
        new("H02", "Transient Humanize", "Minimale, zufällige Transienten-/Peak-Unregelmäßigkeiten."),
        new("H03", "Stereo Drift", "Langsame Mid/Side- und Kanalbalance-Bewegung."),
        new("H04", "Noise Bed", "Sehr leiser analoger Noise-Floor."),
        new("H05", "Groove Drift", "Sehr leichte abschnittsweise Timing-Verschiebung."),
        new("H06", "Human Combo Subtle", "Qualitätsschonende Kombination aus Timing, Stereo, Transienten und Noise."),
        new("H07", "Human Combo Strong", "Stärkerer Diagnosemodus mit deutlicherer Fingerprint-Verschiebung.")
    ];

    public Task<float[]> ApplyAsync(string variationId, float[] interleavedStereo, int sampleRate, CancellationToken ct = default)
    {
        if (interleavedStereo.Length % 2 != 0)
            throw new ArgumentException("Expected interleaved stereo buffer.", nameof(interleavedStereo));

        var x = (float[])interleavedStereo.Clone();

        var result = variationId switch
        {
            "H01" => WowFlutter(x, sampleRate, depthMs: 1.4, rateHz: 0.45),
            "H02" => TransientHumanize(x, strength: 0.018f),
            "H03" => StereoDrift(x, sampleRate, sideDepth: 0.045f, balanceDepth: 0.025f),
            "H04" => AddNoiseBed(x, amount: 0.00028f),
            "H05" => GrooveDrift(x, sampleRate, blockMs: 450, maxShiftMs: 2.2),
            "H06" => HumanCombo(x, sampleRate, strong: false),
            "H07" => HumanCombo(x, sampleRate, strong: true),
            _ => throw new ArgumentException($"Unknown variation id: {variationId}", nameof(variationId))
        };

        return Task.FromResult(result);
    }

    private static float[] HumanCombo(float[] x, int sampleRate, bool strong)
    {
        x = WowFlutter(x, sampleRate, strong ? 2.4 : 1.1, strong ? 0.55 : 0.38);
        x = StereoDrift(x, sampleRate, strong ? 0.075f : 0.035f, strong ? 0.040f : 0.018f);
        x = TransientHumanize(x, strong ? 0.030f : 0.012f);
        x = AddNoiseBed(x, strong ? 0.00042f : 0.00018f);
        x = SoftLimit(x, strong ? 0.965f : 0.985f);
        return x;
    }

    private static float[] AddNoiseBed(float[] x, float amount)
    {
        var rng = new Random(12345);
        var y = new float[x.Length];

        float pinkL = 0, pinkR = 0;

        for (int i = 0; i < x.Length; i += 2)
        {
            pinkL = 0.985f * pinkL + 0.015f * ((float)rng.NextDouble() * 2f - 1f);
            pinkR = 0.985f * pinkR + 0.015f * ((float)rng.NextDouble() * 2f - 1f);

            y[i] = Clamp(x[i] + pinkL * amount);
            y[i + 1] = Clamp(x[i + 1] + pinkR * amount);
        }

        return y;
    }

    private static float[] StereoDrift(float[] x, int sampleRate, float sideDepth, float balanceDepth)
    {
        var y = new float[x.Length];
        double phase1 = 0;
        double phase2 = 1.7;
        double inc1 = 2.0 * Math.PI * 0.031 / sampleRate;
        double inc2 = 2.0 * Math.PI * 0.047 / sampleRate;

        for (int i = 0; i < x.Length; i += 2)
        {
            float l = x[i];
            float r = x[i + 1];

            float mid = (l + r) * 0.5f;
            float side = (l - r) * 0.5f;

            float sideGain = 1f + (float)Math.Sin(phase1) * sideDepth;
            float bal = (float)Math.Sin(phase2) * balanceDepth;

            side *= sideGain;

            l = (mid + side) * (1f + bal);
            r = (mid - side) * (1f - bal);

            y[i] = Clamp(l);
            y[i + 1] = Clamp(r);

            phase1 += inc1;
            phase2 += inc2;
        }

        return y;
    }

    private static float[] TransientHumanize(float[] x, float strength)
    {
        var y = new float[x.Length];
        var rng = new Random(23456);

        float prevL = 0, prevR = 0;

        for (int i = 0; i < x.Length; i += 2)
        {
            float l = x[i];
            float r = x[i + 1];

            float dl = Math.Abs(l - prevL);
            float dr = Math.Abs(r - prevR);
            float transient = MathF.Min(1f, (dl + dr) * 8f);

            float gain = 1f + (((float)rng.NextDouble() * 2f - 1f) * strength * transient);

            y[i] = Clamp(l * gain);
            y[i + 1] = Clamp(r * gain);

            prevL = l;
            prevR = r;
        }

        return y;
    }

    private static float[] GrooveDrift(float[] x, int sampleRate, int blockMs, double maxShiftMs)
    {
        var y = new float[x.Length];
        int frames = x.Length / 2;
        int blockFrames = Math.Max(1, sampleRate * blockMs / 1000);
        var rng = new Random(34567);

        for (int start = 0; start < frames; start += blockFrames)
        {
            int end = Math.Min(frames, start + blockFrames);
            double shiftFrames = ((rng.NextDouble() * 2.0) - 1.0) * maxShiftMs * sampleRate / 1000.0;

            for (int f = start; f < end; f++)
            {
                double src = f + shiftFrames;
                ReadFrameLinear(x, src, out float l, out float r);
                y[f * 2] = l;
                y[f * 2 + 1] = r;
            }
        }

        return y;
    }

    private static float[] WowFlutter(float[] x, int sampleRate, double depthMs, double rateHz)
    {
        var y = new float[x.Length];
        int frames = x.Length / 2;

        double phaseA = 0;
        double phaseB = 1.3;

        double incA = 2.0 * Math.PI * rateHz / sampleRate;
        double incB = 2.0 * Math.PI * (rateHz * 3.7) / sampleRate;

        double depthFrames = depthMs * sampleRate / 1000.0;

        for (int f = 0; f < frames; f++)
        {
            double mod =
                Math.Sin(phaseA) * 0.75 +
                Math.Sin(phaseB) * 0.25;

            double src = f + mod * depthFrames;

            ReadFrameLinear(x, src, out float l, out float r);

            y[f * 2] = l;
            y[f * 2 + 1] = r;

            phaseA += incA;
            phaseB += incB;
        }

        return y;
    }

    private static float[] SoftLimit(float[] x, float ceiling)
    {
        var y = new float[x.Length];

        for (int i = 0; i < x.Length; i++)
        {
            float v = x[i];
            v = MathF.Tanh(v * 1.08f) / MathF.Tanh(1.08f);
            y[i] = Clamp(v * ceiling);
        }

        return y;
    }

    private static void ReadFrameLinear(float[] x, double frame, out float l, out float r)
    {
        int frames = x.Length / 2;

        if (frame <= 0)
        {
            l = x[0];
            r = x[1];
            return;
        }

        if (frame >= frames - 1)
        {
            l = x[(frames - 1) * 2];
            r = x[(frames - 1) * 2 + 1];
            return;
        }

        int i0 = (int)Math.Floor(frame);
        int i1 = i0 + 1;
        float t = (float)(frame - i0);

        float l0 = x[i0 * 2];
        float r0 = x[i0 * 2 + 1];
        float l1 = x[i1 * 2];
        float r1 = x[i1 * 2 + 1];

        l = l0 + (l1 - l0) * t;
        r = r0 + (r1 - r0) * t;
    }

    private static float Clamp(float v)
    {
        if (v > 1f) return 1f;
        if (v < -1f) return -1f;
        return v;
    }
}