namespace Audiola.Services;

public sealed class PythonAudioDetectorProbeProvider(string pythonExe, string scriptPath) : PythonVariationProviderBase(
    name: "Python Audio Detector Probe",
    pythonExe: pythonExe,
    scriptPath: scriptPath,
    variations:
    [
        new AudioVariation("00", "Reference Clean", L.T("Neutraler Re-Export als 32-bit-Float-WAV, ohne Metadaten. Kontrollvariante.")),
        new AudioVariation("01", "Resample 44.1k", L.T("Resampling auf 44.1 kHz. Testet, ob Sample-Rate/Resampling den Detector beeinflusst.")),

        new AudioVariation("02", "Tiny White Noise Floor", L.T("Fügt sehr leises weißes Rauschen hinzu. Kann zu saubere digitale Stille aufbrechen.")),
        new AudioVariation("03", "Tiny Pink Noise Floor", L.T("Fügt sehr leises pinkes Rauschen hinzu. Musikalisch oft unauffälliger als weißes Rauschen.")),

        new AudioVariation("04", "Air Rolloff 18k", L.T("Leichter Lowpass bei 18 kHz. Reduziert sehr hohe digitale Artefakte/Sibilanzbereiche.")),
        new AudioVariation("05", "Air Rolloff 16k", L.T("Stärkerer Lowpass bei 16 kHz. Deutlicherer Eingriff in Höhen und Air-Band.")),

        new AudioVariation("06", "Dynamics Loosen", L.T("Sehr leichte Änderung von Kompression und Limiting. Testet Dynamik-Fingerprint.")),
        new AudioVariation("07", "Stereo Narrow", L.T("Reduziert die Stereo-Seite. Testet Stereo-Breiten- und Mid/Side-Fingerprint.")),
        new AudioVariation("08", "Transient Soften", L.T("Glättet Transienten leicht durch sanfte Kompression. Testet Attack-/Transienten-Erkennung.")),

        new AudioVariation("09", "Tape Soften", L.T("Leichte Echo-/Höhen-Dämpfung für wärmeren, weniger digitalen Klang.")),
        new AudioVariation("10", "Reverb Change", L.T("Fügt ein dezentes Delay/Reverb-ähnliches Tail hinzu. Testet Raum-/Hall-Fingerprint.")),
        new AudioVariation("11", "Mono Compat", L.T("Mischt Links/Rechts stärker zusammen. Testet Mono-Kompatibilität und Kanal-Korrelation.")),
        new AudioVariation("12", "Reamp Sim", L.T("Bandbegrenzung plus leichte Dynamikänderung. Simuliert einfachen Reamp-/Hardware-Charakter.")),

        new AudioVariation("20", "Combined Subtle Fingerprint Shift", L.T("Qualitätsschonende Kombination aus EQ, Dynamik, Stereo, Echo und Limiting.")),
        new AudioVariation("21", "Combined Stronger Fingerprint Shift", L.T("Stärkere Kombination mehrerer Fingerprint-Faktoren bei noch brauchbarer Qualität.")),
        new AudioVariation("22", "Combined Warm Reamp Style", L.T("Warme, reamp-artige Kombivariante mit Höhenbegrenzung und Dynamikänderung.")),

        new AudioVariation("23", "Resample 44.1 -> 48", L.T("Resampling erst auf 44.1 kHz und zurück auf 48 kHz. Testet Resampling-Artefakte.")),
        new AudioVariation("24", "Micro Pitch Shift", L.T("Minimale Tonhöhenverschiebung mit Tempo-Korrektur. Testet Pitch-/Formant-nahe Fingerprints.")),
        new AudioVariation("25", "Micro Timing Shift", L.T("Minimale Timing-/Resampling-Änderung. Testet zeitbasierte Fingerprints.")),
        new AudioVariation("26", "Subtle Pink Noise Bed", L.T("Sehr dezentes pinkes Rauschen als analog wirkender Noise Floor.")),

        new AudioVariation("27", "Phase Decorrelate Subtle", L.T("Leichte Phasen-Dekorrelation im Stereobild. Testet Kanal- und Phasenmerkmale.")),
        new AudioVariation("28", "Mid/Side Rebalance", L.T("Verschiebt das Verhältnis von Mitte und Seite. Oft relativ qualitätsschonend.")),
        new AudioVariation("29", "Soft Clip Safe", L.T("Sehr sanftes Limiting/Soft-Clipping-Verhalten. Testet Peak- und Loudness-Fingerprint.")),

        new AudioVariation("30", "Tiny Telephone Parallel", L.T("Mischt extrem leise eine bandbegrenzte Telefonspur parallel dazu. Starker Fingerprint-Test.")),
        new AudioVariation("31", "Tiny Room Parallel", L.T("Mischt sehr leise einen kleinen Raum-/Delay-Anteil parallel dazu. Meist musikalischer als 30.")),

        new AudioVariation("32", "Combined Quality Preserving", L.T("Beste Allround-Variante: mehrere subtile Änderungen mit möglichst wenig Qualitätsverlust.")),
        new AudioVariation("33", "Combined Heavy Fingerprint Shift", L.T("Deutlichere Fingerprint-Verschiebung. Eher Diagnose-Variante als finaler Master.")),

        new AudioVariation("40", "Suno Cleanup Subtle", L.T("Suno-spezifischer Cleanup-Test mit leichtem Höhen-Rolloff, kleinen EQ-Dips, Stereo-Anpassung und sanfter Dynamikänderung.")),
        new AudioVariation("41", "Suno Cleanup Medium", L.T("Stärkerer Cleanup-Test gegen Höhen-/Sibilanz-Artefakte, Stereo-Pattern, Phasenmerkmale und glatte Dynamik.")),
        new AudioVariation("42", "Suno Cleanup Heavy", L.T("Deutlicher Diagnose-Eingriff gegen mögliche Suno-ähnliche Spektral-, Stereo- und Dynamikmuster. Nicht unbedingt final-master-tauglich.")),
        new AudioVariation("43", "Suno Cleanup Parallel Room", L.T("Mischt einen leisen bandbegrenzten Raumanteil parallel dazu und verändert den Dry-Anteil. Testet Raum-/Hall- und Spektral-Fingerprint.")),
        new AudioVariation("44", "Suno Cleanup Resample Phase", L.T("Kombiniert Resampling, leichte Phasenänderung, Stereo-Anpassung und Höhenbegrenzung.")),
        new AudioVariation("45", "Suno Cleanup Best Quality", L.T("Qualitätsschonendster Suno-Cleanup-Modus. Guter erster Kandidat für einen brauchbaren alternativen Master."))
    ]);