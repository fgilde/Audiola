namespace Audiola.Services;

public sealed class PythonAudioDetectorProbeProvider(string pythonExe, string scriptPath) : PythonVariationProviderBase(
    name: "Python Audio Detector Probe",
    pythonExe: pythonExe,
    scriptPath: scriptPath,
    variations:
    [
        new AudioVariation("00", "Reference Clean", "Neutraler Re-Export als 32-bit-Float-WAV, ohne Metadaten. Kontrollvariante."),
        new AudioVariation("01", "Resample 44.1k", "Resampling auf 44.1 kHz. Testet, ob Sample-Rate/Resampling den Detector beeinflusst."),

        new AudioVariation("02", "Tiny White Noise Floor", "Fügt sehr leises weißes Rauschen hinzu. Kann zu saubere digitale Stille aufbrechen."),
        new AudioVariation("03", "Tiny Pink Noise Floor", "Fügt sehr leises pinkes Rauschen hinzu. Musikalisch oft unauffälliger als weißes Rauschen."),

        new AudioVariation("04", "Air Rolloff 18k", "Leichter Lowpass bei 18 kHz. Reduziert sehr hohe digitale Artefakte/Sibilanzbereiche."),
        new AudioVariation("05", "Air Rolloff 16k", "Stärkerer Lowpass bei 16 kHz. Deutlicherer Eingriff in Höhen und Air-Band."),

        new AudioVariation("06", "Dynamics Loosen", "Sehr leichte Änderung von Kompression und Limiting. Testet Dynamik-Fingerprint."),
        new AudioVariation("07", "Stereo Narrow", "Reduziert die Stereo-Seite. Testet Stereo-Breiten- und Mid/Side-Fingerprint."),
        new AudioVariation("08", "Transient Soften", "Glättet Transienten leicht durch sanfte Kompression. Testet Attack-/Transienten-Erkennung."),

        new AudioVariation("09", "Tape Soften", "Leichte Echo-/Höhen-Dämpfung für wärmeren, weniger digitalen Klang."),
        new AudioVariation("10", "Reverb Change", "Fügt ein dezentes Delay/Reverb-ähnliches Tail hinzu. Testet Raum-/Hall-Fingerprint."),
        new AudioVariation("11", "Mono Compat", "Mischt Links/Rechts stärker zusammen. Testet Mono-Kompatibilität und Kanal-Korrelation."),
        new AudioVariation("12", "Reamp Sim", "Bandbegrenzung plus leichte Dynamikänderung. Simuliert einfachen Reamp-/Hardware-Charakter."),

        new AudioVariation("20", "Combined Subtle Fingerprint Shift", "Qualitätsschonende Kombination aus EQ, Dynamik, Stereo, Echo und Limiting."),
        new AudioVariation("21", "Combined Stronger Fingerprint Shift", "Stärkere Kombination mehrerer Fingerprint-Faktoren bei noch brauchbarer Qualität."),
        new AudioVariation("22", "Combined Warm Reamp Style", "Warme, reamp-artige Kombivariante mit Höhenbegrenzung und Dynamikänderung."),

        new AudioVariation("23", "Resample 44.1 → 48", "Resampling erst auf 44.1 kHz und zurück auf 48 kHz. Testet Resampling-Artefakte."),
        new AudioVariation("24", "Micro Pitch Shift", "Minimale Tonhöhenverschiebung mit Tempo-Korrektur. Testet Pitch-/Formant-nahe Fingerprints."),
        new AudioVariation("25", "Micro Timing Shift", "Minimale Timing-/Resampling-Änderung. Testet zeitbasierte Fingerprints."),
        new AudioVariation("26", "Subtle Pink Noise Bed", "Sehr dezentes pinkes Rauschen als analog wirkender Noise Floor."),

        new AudioVariation("27", "Phase Decorrelate Subtle", "Leichte Phasen-Dekorrelation im Stereobild. Testet Kanal- und Phasenmerkmale."),
        new AudioVariation("28", "Mid/Side Rebalance", "Verschiebt das Verhältnis von Mitte und Seite. Oft relativ qualitätsschonend."),
        new AudioVariation("29", "Soft Clip Safe", "Sehr sanftes Limiting/Soft-Clipping-Verhalten. Testet Peak- und Loudness-Fingerprint."),

        new AudioVariation("30", "Tiny Telephone Parallel", "Mischt extrem leise eine bandbegrenzte Telefonspur parallel dazu. Starker Fingerprint-Test."),
        new AudioVariation("31", "Tiny Room Parallel", "Mischt sehr leise einen kleinen Raum-/Delay-Anteil parallel dazu. Meist musikalischer als 30."),

        new AudioVariation("32", "Combined Quality Preserving", "Beste Allround-Variante: mehrere subtile Änderungen mit möglichst wenig Qualitätsverlust."),
        new AudioVariation("33", "Combined Heavy Fingerprint Shift", "Deutlichere Fingerprint-Verschiebung. Eher Diagnose-Variante als finaler Master.")
    ]);