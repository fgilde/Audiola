using System.IO;

namespace Audiola.Services;

/// <summary>Eine Spur für den Offline-Mix: Quelle + Lautstärke/Pan/Mute/Solo/Offset.</summary>
public sealed record MixTrack(
    string FilePath,
    double Volume = 1.0,
    double Pan = 0.0,
    bool Muted = false,
    bool Solo = false,
    double OffsetSeconds = 0.0);

/// <summary>
/// Rendert mehrere Spuren offline zu einem Stereo-Mix (Resampling auf eine gemeinsame Rate,
/// Constant-Power-Pan wie im WPF-Client, Summierung mit Offset, Brickwall-Schutz). Reine Logik,
/// von Desktop und Web nutzbar.
/// </summary>
public static class OfflineMixer
{
    public static (float[] Samples, int SampleRate) Render(IReadOnlyList<MixTrack> tracks)
    {
        var anySolo = tracks.Any(t => t.Solo);

        var loaded = new List<(float[] Samples, int Sr, MixTrack Track)>();
        var target = 0;
        foreach (var t in tracks)
        {
            if (t.Muted || (anySolo && !t.Solo)) continue;
            if (string.IsNullOrEmpty(t.FilePath) || !File.Exists(t.FilePath)) continue;
            var (s, sr) = AudioProcessingHelper.ReadStereo(t.FilePath);
            if (target == 0) target = sr;
            loaded.Add((s, sr, t));
        }
        if (loaded.Count == 0) return ([], target == 0 ? 44100 : target);

        var prepared = new List<(float[] Samples, long OffsetSamples, MixTrack Track)>();
        long maxLen = 0;
        foreach (var (s, sr, t) in loaded)
        {
            var rs = sr == target ? s : AudioProcessingHelper.Resample(s, sr, target);
            var offsetSamples = (long)(Math.Max(0, t.OffsetSeconds) * target) * 2;
            prepared.Add((rs, offsetSamples, t));
            maxLen = Math.Max(maxLen, offsetSamples + rs.Length);
        }

        var mix = new float[maxLen];
        foreach (var (s, offsetSamples, t) in prepared)
        {
            var vol = (float)Math.Max(0, t.Volume);
            var pan = Math.Clamp(t.Pan, -1, 1);
            var angle = (pan + 1) * 0.25 * Math.PI;          // 0..π/2
            var lGain = (float)Math.Cos(angle) * vol;        // center = 0.7071 (−3 dB)
            var rGain = (float)Math.Sin(angle) * vol;

            for (var i = 0; i + 1 < s.Length; i += 2)
            {
                var idx = offsetSamples + i;
                mix[idx] += s[i] * lGain;
                mix[idx + 1] += s[i + 1] * rGain;
            }
        }

        for (var i = 0; i < mix.Length; i++)
            mix[i] = Math.Clamp(mix[i], -1f, 1f);

        return (mix, target);
    }
}
