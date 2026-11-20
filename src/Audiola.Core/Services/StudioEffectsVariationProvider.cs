namespace Audiola.Services;

/// <summary>
/// Beispiel-Provider: bietet ein paar kreative Bearbeitungen auf Basis der vorhandenen
/// <see cref="AudioEffects"/>-DSP. Dient als Vorlage für eigene Provider-Implementierungen.
/// </summary>
public sealed class StudioEffectsVariationProvider : IAudioVariationProvider
{
    public string Name => "Studio-Effekte";

    private static readonly AudioVariation[] Variations =
    [
        new("reverb",    L.T("Hall (sanft)"),      L.T("Dezenter Raum/Hall (Schroeder-Reverb, Wet ~25 %).")),
        new("echo",      L.T("Echo/Delay"),        L.T("Slapback-artiges Delay mit Feedback.")),
        new("wide",      L.T("Stereo breiter"),    L.T("Verbreitert das Stereobild (Mid/Side, Faktor 1.4).")),
        new("cleanup",   L.T("Vocal-Cleanup"),     L.T("De-Esser, Härte zähmen, sanfte Kompression.")),
        new("normalize", L.T("Normalisieren"),     L.T("Peak-Normalisierung auf −1 dBFS.")),
        new("reverse",   L.T("Umkehren"),          L.T("Spielt das Material rückwärts.")),
    ];

    public IReadOnlyList<AudioVariation> GetVariations() => Variations;

    public Task<float[]> ApplyAsync(string variationId, float[] s, int sampleRate, CancellationToken ct = default)
        => Task.Run(() =>
        {
            var frames = s.Length / 2;
            return variationId switch
            {
                "reverb"    => AudioEffects.Reverb(s, 0, frames, sampleRate, 0.25),
                "echo"      => AudioEffects.Echo(s, 0, frames, sampleRate),
                "wide"      => AudioEffects.StereoWiden(s, 0, frames, 1.4),
                "cleanup"   => AudioEffects.VocalCleanup(s, 0, frames, sampleRate, 1.0),
                "normalize" => AudioEffects.Normalize(s, 0, frames, -1),
                "reverse"   => AudioEffects.Reverse(s, 0, frames),
                _           => (float[])s.Clone()
            };
        }, ct);
}
