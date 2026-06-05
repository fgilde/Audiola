namespace Audiola.Services;

public sealed class PythonAudioDetectorProbeProvider(string pythonExe, string scriptPath) : PythonVariationProviderBase(
    name: "Python Audio Detector Probe",
    pythonExe: pythonExe,
    scriptPath: scriptPath,
    variations:
    [
        new AudioVariation("00", "Reference Clean", "Nur Re-Export ohne Metadaten."),
        new AudioVariation("01", "Resample 44.1k", "Resampling auf 44.1 kHz."),
        new AudioVariation("02", "Tiny Noise Floor", "Sehr leichter weißer Noise Floor."),
        new AudioVariation("03", "Pink Noise Floor", "Sehr leichter pinker Noise Floor."),
        new AudioVariation("04", "Air Rolloff 18k", "Leichter Lowpass bei 18 kHz."),
        new AudioVariation("05", "Air Rolloff 16k", "Stärkerer Lowpass bei 16 kHz."),
        new AudioVariation("06", "Dynamics Loosen", "Leichte Dynamikänderung."),
        new AudioVariation("07", "Stereo Narrow", "Stereo-Breite reduziert."),
        new AudioVariation("08", "Transient Soften", "Transienten leicht geglättet."),
        new AudioVariation("09", "Tape Soften", "Wärmerer, leicht gedämpfter Sound."),
        new AudioVariation("10", "Reverb Change", "Leichte Raum-/Delay-Änderung."),
        new AudioVariation("11", "Mono Compat", "Mehr Mono-Kompatibilität."),
        new AudioVariation("12", "Reamp Sim", "Reamp-artige Filter-/Dynamikänderung."),
        new AudioVariation("20", "Combined Subtle Fingerprint Shift", "Kombiniert mehrere subtile Änderungen."),
        new AudioVariation("21", "Combined Stronger Fingerprint Shift", "Kombiniert mehrere stärkere Änderungen."),
        new AudioVariation("22", "Combined Warm Reamp Style", "Warme Reamp-artige Kombivariante.")
    ]);