namespace Audiola.Dsp;

/// <summary>
/// Grundfrequenz-Schätzung (F0) per YIN-Algorithmus — für die Referenz-Melodie (Gesangsspur offline)
/// und den Echtzeit-Mikrofon-Ton beim Einsingen. Genau genug für Karaoke-Scoring, ohne externe Abhängigkeit.
/// Plus Umrechnungen zwischen Hz, MIDI-Note und Notennamen.
/// </summary>
public static class PitchDetector
{
    /// <summary>
    /// Schätzt die Grundfrequenz (Hz) eines Mono-Blocks. Gibt 0 zurück, wenn kein klarer Ton
    /// erkennbar ist (stimmlos/Stille) — dann sollte kein Treffer gewertet werden.
    /// </summary>
    /// <param name="buf">Mono-Samples (−1..1). Länge ≈ 2048–4096 für Stimme sinnvoll.</param>
    /// <param name="sampleRate">Abtastrate in Hz.</param>
    /// <param name="minHz">Untere Grenze (Standard 65 Hz ≈ C2, tiefe Männerstimme).</param>
    /// <param name="maxHz">Obere Grenze (Standard 1200 Hz, deckt Sopran ab).</param>
    /// <param name="threshold">YIN-Absolutschwelle (0.1–0.2; kleiner = strenger).</param>
    public static float DetectHz(ReadOnlySpan<float> buf, int sampleRate,
        float minHz = 65f, float maxHz = 1200f, float threshold = 0.15f)
    {
        int tauMin = Math.Max(2, (int)(sampleRate / maxHz));
        int tauMax = Math.Min(buf.Length / 2, (int)(sampleRate / minHz));
        if (tauMax <= tauMin) return 0f;

        // Zu leise? Dann gar nicht erst suchen (spart Rechenzeit, vermeidet Rausch-Treffer).
        double rms = 0;
        for (int i = 0; i < buf.Length; i++) rms += buf[i] * buf[i];
        rms = Math.Sqrt(rms / buf.Length);
        if (rms < 0.005) return 0f;

        // 1) Differenzfunktion d(τ).
        var d = new float[tauMax];
        for (int tau = tauMin; tau < tauMax; tau++)
        {
            float sum = 0f;
            int n = buf.Length - tau;
            for (int j = 0; j < n; j++)
            {
                float diff = buf[j] - buf[j + tau];
                sum += diff * diff;
            }
            d[tau] = sum;
        }

        // 2) Kumulative mittlere normalisierte Differenz d'(τ).
        var dn = new float[tauMax];
        dn[tauMin] = 1f;
        float running = 0f;
        for (int tau = tauMin + 1; tau < tauMax; tau++)
        {
            running += d[tau];
            dn[tau] = running > 0f ? d[tau] * (tau - tauMin) / running : 1f;
        }

        // 3) Erstes lokales Minimum unter der Schwelle.
        int bestTau = -1;
        for (int tau = tauMin + 1; tau < tauMax - 1; tau++)
        {
            if (dn[tau] < threshold)
            {
                while (tau + 1 < tauMax && dn[tau + 1] < dn[tau]) tau++;
                bestTau = tau;
                break;
            }
        }
        if (bestTau < 0) return 0f; // kein klarer periodischer Anteil → stimmlos

        // 4) Parabolische Interpolation um das Minimum (Sub-Sample-Genauigkeit).
        float betterTau = bestTau;
        if (bestTau > tauMin && bestTau < tauMax - 1)
        {
            float a = dn[bestTau - 1], b = dn[bestTau], c = dn[bestTau + 1];
            float denom = a + c - 2f * b;
            if (Math.Abs(denom) > 1e-9f) betterTau = bestTau + (a - c) / (2f * denom);
        }
        return betterTau > 0f ? sampleRate / betterTau : 0f;
    }

    private static readonly string[] Names =
        ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    /// <summary>Frequenz (Hz) → MIDI-Notennummer (A4 = 69 = 440 Hz), als Fließkomma (mit Cent-Anteil).</summary>
    public static double HzToMidi(double hz) => hz <= 0 ? 0 : 69.0 + 12.0 * Math.Log2(hz / 440.0);

    /// <summary>MIDI-Notennummer → Frequenz (Hz).</summary>
    public static double MidiToHz(double midi) => 440.0 * Math.Pow(2.0, (midi - 69.0) / 12.0);

    /// <summary>Notenname inkl. Oktave, z. B. "A4", "C#3".</summary>
    public static string MidiToName(double midi)
    {
        int m = (int)Math.Round(midi);
        int octave = m / 12 - 1;
        return Names[((m % 12) + 12) % 12] + octave;
    }

    /// <summary>
    /// Abweichung zweier Frequenzen in Cent (100 Cent = ein Halbton). Oktavfehler werden ausgeglichen,
    /// damit „richtige Note, falsche Oktave" trotzdem als Treffer zählt (typisch beim Einsingen).
    /// </summary>
    public static double CentsOffOctaveless(double sungHz, double targetHz)
    {
        if (sungHz <= 0 || targetHz <= 0) return double.NaN;
        double semis = 12.0 * Math.Log2(sungHz / targetHz);
        double mod = ((semis % 12) + 12) % 12;      // in [0,12)
        if (mod > 6) mod -= 12;                       // nächstgelegene Oktave
        return mod * 100.0;
    }
}
