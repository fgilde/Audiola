using Audiola.ViewModels;
using NAudio.Wave;

namespace Audiola.Services;

/// <summary>
/// Liest einen Stem (Stereo) und wendet live die Mixer-Parameter an:
/// Enabled/Mute/Solo entscheiden über aktiv, Volume/Pan über den Gain.
/// Die Werte werden bei jedem Block frisch aus dem <see cref="StemTrackViewModel"/>
/// gelesen, sodass Reglerbewegungen sofort hörbar sind.
/// </summary>
internal sealed class LiveStemSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly StemTrackViewModel _vm;
    private readonly IReadOnlyList<StemTrackViewModel> _all;

    public LiveStemSampleProvider(
        ISampleProvider source,
        StemTrackViewModel vm,
        IReadOnlyList<StemTrackViewModel> all)
    {
        _source = source;
        _vm = vm;
        _all = all;
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        var read = _source.Read(buffer, offset, count);

        var anySolo = false;
        for (var i = 0; i < _all.Count; i++)
            if (_all[i].IsSolo) { anySolo = true; break; }

        var active = _vm.IsEnabled && !_vm.IsMuted && (!anySolo || _vm.IsSolo);
        var volume = active ? (float)_vm.Volume : 0f;

        // Constant-Power-Panning.
        var angle = ((float)_vm.Pan + 1f) * 0.25f * MathF.PI;
        var leftGain = volume * MathF.Cos(angle);
        var rightGain = volume * MathF.Sin(angle);

        var peak = 0f;
        for (var i = 0; i < read; i += 2)
        {
            buffer[offset + i] *= leftGain;
            if (i + 1 < read)
                buffer[offset + i + 1] *= rightGain;

            var a = Math.Abs(buffer[offset + i]);
            if (a > peak) peak = a;
        }

        // Spitzenpegel für die VU-Anzeige (vom UI-Timer abgeholt).
        if (peak > _vm.MeterPeak) _vm.MeterPeak = peak;

        return read;
    }
}
