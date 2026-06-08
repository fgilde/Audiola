using Audiola.Dsp;
using Audiola.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Audiola.Services;

/// <summary>Offline-Mastering-Verarbeitung auf Basis von NAudio + eigener DSP.</summary>
public sealed class MasteringService : IMasteringService
{
    public Task<double> MeasureLufsAsync(string filePath, CancellationToken ct = default)
        => Task.Run(() =>
        {
            var (samples, sampleRate) = AudioProcessingHelper.ReadStereo(filePath);
            return LoudnessMeter.MeasureIntegratedLufs(samples, sampleRate);
        }, ct);

    public Task<MasteringResult> ProcessAndExportAsync(
        string inputFile, string outputFile, MasteringSettings s, CancellationToken ct = default)
        => Task.Run(() =>
        {
            var (samples, sr, result) = ProcessBuffer(inputFile, s, ct);
            AudioExporter.Export(new FloatArraySampleProvider(samples, sr, 2), outputFile);
            return result;
        }, ct);

    /// <summary>Verarbeitet die Datei und gibt die Wellenform-Peaks des Ergebnisses zurück.</summary>
    public Task<float[]> ProcessPeaksAsync(string inputFile, MasteringSettings s, int targetBuckets = 2000, CancellationToken ct = default)
        => Task.Run(() =>
        {
            var (samples, _, _) = ProcessBuffer(inputFile, s, ct);
            return AudioEdits.ComputePeaks(samples, targetBuckets);
        }, ct);

    private static (float[] Samples, int SampleRate, MasteringResult Result) ProcessBuffer(
        string inputFile, MasteringSettings s, CancellationToken ct)
    {
        var (samples, sampleRate) = AudioProcessingHelper.ReadStereo(inputFile);
        var inputLufs = LoudnessMeter.MeasureIntegratedLufs(samples, sampleRate);

        // EQ-Filter pro Kanal aufbauen.
        var chainL = BuildEq(s, sampleRate);
        var chainR = BuildEq(s, sampleRate);
        var comp = s.CompressorEnabled
            ? new Compressor(sampleRate, s.ThresholdDb, s.Ratio, s.AttackMs, s.ReleaseMs, s.MakeupGainDb)
            : null;

        var frames = samples.Length / 2;
        for (var i = 0; i < frames; i++)
        {
            ct.ThrowIfCancellationRequested();

            var l = samples[i * 2];
            var r = samples[i * 2 + 1];

            foreach (var f in chainL) l = f.Process(l);
            foreach (var f in chainR) r = f.Process(r);

            if (comp is not null)
                comp.Process(ref l, ref r);

            samples[i * 2] = l;
            samples[i * 2 + 1] = r;
        }

        // LUFS-Normalisierung auf Zielwert.
        var appliedGainDb = 0.0;
        if (s.NormalizeLoudness)
        {
            appliedGainDb = AudioProcessingHelper.ApplyLoudnessTarget(samples, sampleRate, s.TargetLufs);
        }

        // Brickwall-Clipping-Schutz + Zählung.
        var clipped = AudioProcessingHelper.ApplyBrickwallClip(samples);

        var outputLufs = LoudnessMeter.MeasureIntegratedLufs(samples, sampleRate);

        return (samples, sampleRate, new MasteringResult(inputLufs, outputLufs, appliedGainDb, clipped));
    }

    private static List<Biquad> BuildEq(MasteringSettings s, int fs)
    {
        var chain = new List<Biquad>();
        if (s.HighPassEnabled)
            chain.Add(Biquad.HighPass(fs, s.HighPassHz, 0.707));
        if (Math.Abs(s.LowShelfGainDb) > 0.01)
            chain.Add(Biquad.LowShelf(fs, s.LowShelfHz, 0.707, s.LowShelfGainDb));
        if (Math.Abs(s.MidGainDb) > 0.01)
            chain.Add(Biquad.Peaking(fs, s.MidHz, s.MidQ, s.MidGainDb));
        if (Math.Abs(s.HighShelfGainDb) > 0.01)
            chain.Add(Biquad.HighShelf(fs, s.HighShelfHz, 0.707, s.HighShelfGainDb));
        return chain;
    }

}
