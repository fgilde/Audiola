using Audiola.Dsp;
using Audiola.Models;

namespace Audiola.Services;

/// <summary>
/// Erzeugt konservative, klangnahe Audio-Varianten ohne Pitch- oder Tempo-Aenderung.
/// </summary>
public sealed class VariationService : IVariationService
{
    public Task<VariationResult> ProcessAndExportAsync(
        string inputFile,
        string outputFile,
        VariationSettings settings,
        CancellationToken ct = default)
        => Task.Run(() => ProcessAndExport(inputFile, outputFile, settings, ct), ct);

    private static VariationResult ProcessAndExport(
        string inputFile,
        string outputFile,
        VariationSettings settings,
        CancellationToken ct)
    {
        var (samples, sampleRate) = AudioProcessingHelper.ReadStereo(inputFile);
        var inputLufs = LoudnessMeter.MeasureIntegratedLufs(samples, sampleRate);
        var intensity = Math.Clamp(settings.Intensity, 0, 1);

        var eqLeft = BuildEq(settings, sampleRate, intensity);
        var eqRight = BuildEq(settings, sampleRate, intensity);

        var widthAmount = Math.Clamp(settings.StereoWidth, -1, 1);
        var sideGain = Math.Clamp(1.0 + (0.22 * intensity * widthAmount), 0.82, 1.24);
        var stereoWidthSkipped = Math.Abs(widthAmount) > 0.001 && HasVeryLowSideEnergy(samples);

        var saturationAmount = Math.Clamp(settings.Saturation, 0, 1) * intensity;
        var saturationMix = 0.14 * saturationAmount;
        var saturationDrive = 1.0 + (0.45 * saturationAmount);
        var saturationNormalizer = Math.Tanh(saturationDrive);

        var textureAmount = Math.Clamp(settings.Texture, 0, 1) * intensity;
        var textureLevel = (float)(0.00035 * textureAmount);
        var random = new Random(settings.Seed);
        var noiseLeft = 0f;
        var noiseRight = 0f;

        var frames = samples.Length / 2;
        for (var frame = 0; frame < frames; frame++)
        {
            ct.ThrowIfCancellationRequested();

            var index = frame * 2;
            var left = samples[index];
            var right = samples[index + 1];

            foreach (var filter in eqLeft)
                left = filter.Process(left);

            foreach (var filter in eqRight)
                right = filter.Process(right);

            if (!stereoWidthSkipped && Math.Abs(widthAmount) > 0.001)
            {
                var mid = (left + right) * 0.5f;
                var side = (left - right) * 0.5f;
                side *= (float)sideGain;
                left = mid + side;
                right = mid - side;
            }

            if (saturationMix > 0.0001)
            {
                left = BlendSaturation(left, saturationDrive, saturationNormalizer, saturationMix);
                right = BlendSaturation(right, saturationDrive, saturationNormalizer, saturationMix);
            }

            if (textureLevel > 0f)
            {
                noiseLeft = 0.985f * noiseLeft + 0.015f * NextCentered(random);
                noiseRight = 0.985f * noiseRight + 0.015f * NextCentered(random);
                left += noiseLeft * textureLevel;
                right += noiseRight * textureLevel;
            }

            samples[index] = left;
            samples[index + 1] = right;
        }

        var appliedLoudnessGainDb = 0.0;
        if (settings.MatchInputLoudness && !double.IsInfinity(inputLufs))
            appliedLoudnessGainDb = AudioProcessingHelper.ApplyLoudnessTarget(samples, sampleRate, inputLufs);

        var clippedSamples = AudioProcessingHelper.ApplyBrickwallClip(samples);
        var outputLufs = LoudnessMeter.MeasureIntegratedLufs(samples, sampleRate);

        var provider = new FloatArraySampleProvider(samples, sampleRate, 2);
        AudioExporter.Export(provider, outputFile);

        return new VariationResult(
            inputLufs,
            outputLufs,
            appliedLoudnessGainDb,
            clippedSamples,
            stereoWidthSkipped,
            settings.Seed);
    }

    private static List<Biquad> BuildEq(VariationSettings settings, int sampleRate, double intensity)
    {
        var chain = new List<Biquad>();

        var timbre = Math.Clamp(settings.TimbreShift, -1, 1);
        if (Math.Abs(timbre) > 0.001)
        {
            var lowShelfDb = -1.8 * timbre * intensity;
            var highShelfDb = 1.8 * timbre * intensity;
            chain.Add(Biquad.LowShelf(sampleRate, 180, 0.707, lowShelfDb));
            chain.Add(Biquad.HighShelf(sampleRate, 6500, 0.707, highShelfDb));
        }

        var presence = Math.Clamp(settings.PresenceShift, -1, 1);
        if (Math.Abs(presence) > 0.001)
        {
            var presenceDb = 1.2 * presence * intensity;
            chain.Add(Biquad.Peaking(sampleRate, 2800, 0.8, presenceDb));
        }

        return chain;
    }

    private static bool HasVeryLowSideEnergy(float[] samples)
    {
        double midEnergy = 0;
        double sideEnergy = 0;

        for (var i = 0; i < samples.Length - 1; i += 2)
        {
            var mid = (samples[i] + samples[i + 1]) * 0.5;
            var side = (samples[i] - samples[i + 1]) * 0.5;
            midEnergy += mid * mid;
            sideEnergy += side * side;
        }

        return sideEnergy < (midEnergy * 1e-6);
    }

    private static float BlendSaturation(float sample, double drive, double normalizer, double mix)
    {
        var saturated = (float)(Math.Tanh(sample * drive) / normalizer);
        return AudioProcessingHelper.Lerp(sample, saturated, mix);
    }

    private static float NextCentered(Random random)
        => (random.NextSingle() * 2f) - 1f;
}
