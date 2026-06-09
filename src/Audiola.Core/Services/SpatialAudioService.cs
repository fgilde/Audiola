using NAudio.Wave;

namespace Audiola.Services;

/// <summary>Kanal-Layout für den Mehrkanal-Export.</summary>
public enum SpatialLayout
{
    Surround51,   // 6 Kanäle: FL FR FC LFE SL SR
    Surround71,   // 8 Kanäle: FL FR FC LFE BL BR SL SR
    Atmos714      // 12 Kanäle: 7.1-Bett + 4 Höhenkanäle (Atmos-Bed-Layout)
}

/// <summary>
/// Eine im Raum platzierte Quelle. Azimut: 0° = vorn, +90° = rechts, −90° = links.
/// Elevation: 0° = Ohrhöhe, +90° = direkt über dem Kopf. Distanz ≈ 1.0 = neutral.
/// </summary>
public sealed record SpatialSource(
    string FilePath, double AzimuthDeg, double ElevationDeg, double Distance, double GainDb, bool Muted);

/// <summary>
/// Räumlicher Renderer ohne Lizenz-Encoder: erzeugt entweder einen <b>binauralen</b>
/// 3D-Mix (Kopfhörer, HRTF-Näherung mit ITD/ILD + Kopf-Shadow-Tiefpass) oder eine
/// <b>Mehrkanal-WAV</b> (5.1 / 7.1 / 7.1.4 = Atmos-Bett-Layout) per VBAP-artiger
/// Verstärkungsverteilung auf der Kugel.
/// </summary>
public static class SpatialAudioService
{
    private const int Sr = 48000;

    // -------- Binaural (Kopfhörer) --------

    public static (float[] Interleaved, int SampleRate) RenderBinaural(
        IReadOnlyList<SpatialSource> sources, double roomAmount = 0.18)
    {
        var monos = LoadMonos(sources, out var maxLen);
        var maxItd = (int)(0.0007 * Sr);          // max. interaurale Laufzeitdifferenz (~0,7 ms)
        var outLen = maxLen + maxItd + 1;
        var left = new float[outLen];
        var right = new float[outLen];

        for (var s = 0; s < sources.Count; s++)
        {
            var src = sources[s];
            var mono = monos[s];
            if (src.Muted || mono.Length == 0) continue;

            var az = src.AzimuthDeg * Math.PI / 180.0;
            var el = src.ElevationDeg * Math.PI / 180.0;
            var sinAz = Math.Sin(az);
            var elFactor = Math.Cos(el);                          // über Kopf → mittiger
            var g = Math.Pow(10, src.GainDb / 20.0) / Math.Clamp(src.Distance, 0.3, 3.0);

            var itd = maxItd * sinAz * elFactor;
            var delayL = itd > 0 ? (int)Math.Round(itd) : 0;      // Quelle rechts → links verzögert
            var delayR = itd < 0 ? (int)Math.Round(-itd) : 0;

            var ildHalf = 4.5 * sinAz * elFactor;                 // dB pro Ohr (Pegel-Differenz)
            var gL = g * Math.Pow(10, -ildHalf / 20.0);
            var gR = g * Math.Pow(10, +ildHalf / 20.0);

            var shadow = Math.Abs(sinAz);                         // Kopf-Shadow auf dem fernen Ohr
            var fc = 19000 - 14000 * shadow;
            var alpha = Math.Exp(-2 * Math.PI * fc / Sr);
            var farIsLeft = sinAz > 0;
            double lpFar = 0;

            for (var n = 0; n < mono.Length; n++)
            {
                var m = mono[n];
                lpFar = (1 - alpha) * m + alpha * lpFar;          // gedämpftes fernes Ohr
                var far = (float)lpFar;
                if (farIsLeft)
                {
                    right[n + delayR] += (float)(gR * m);
                    left[n + delayL] += (float)(gL * far);
                }
                else
                {
                    left[n + delayL] += (float)(gL * m);
                    right[n + delayR] += (float)(gR * far);
                }
            }
        }

        var inter = new float[outLen * 2];
        for (var n = 0; n < outLen; n++) { inter[n * 2] = left[n]; inter[n * 2 + 1] = right[n]; }

        if (roomAmount > 0.001)
            inter = AudioEffects.Reverb(inter, 0, outLen, Sr, Math.Clamp(roomAmount, 0, 0.6));

        NormalizePeak(inter, 0.97f);
        return (inter, Sr);
    }

    // -------- Mehrkanal (Surround / Atmos-Bett) --------

    public static (float[] Interleaved, int Channels, int SampleRate) RenderMultichannel(
        IReadOnlyList<SpatialSource> sources, SpatialLayout layout)
    {
        var (spk, lfeIndex) = Speakers(layout);
        var ch = spk.Length;
        var sv = new (double X, double Y, double Z)[ch];
        for (var c = 0; c < ch; c++) sv[c] = Unit(spk[c].Az, spk[c].El);

        var monos = LoadMonos(sources, out var outLen);
        var bus = new float[ch][];
        for (var c = 0; c < ch; c++) bus[c] = new float[outLen];

        for (var s = 0; s < sources.Count; s++)
        {
            var src = sources[s];
            var mono = monos[s];
            if (src.Muted || mono.Length == 0) continue;

            var g = Math.Pow(10, src.GainDb / 20.0) / Math.Clamp(src.Distance, 0.3, 3.0);
            var u = Unit(src.AzimuthDeg, src.ElevationDeg);

            var gains = new double[ch];
            var sumSq = 0.0;
            for (var c = 0; c < ch; c++)
            {
                if (c == lfeIndex) continue;
                var d = u.X * sv[c].X + u.Y * sv[c].Y + u.Z * sv[c].Z;
                var gg = Math.Pow(Math.Max(0, d), 1.6);
                gains[c] = gg;
                sumSq += gg * gg;
            }
            var norm = sumSq > 1e-9 ? 1.0 / Math.Sqrt(sumSq) : 0;   // konstante Leistung
            for (var c = 0; c < ch; c++) gains[c] *= norm * g;

            var alpha = Math.Exp(-2 * Math.PI * 120 / Sr);          // LFE-Tiefpass ~120 Hz
            var lfeGain = 0.4 * g;
            double lp = 0;

            for (var n = 0; n < mono.Length; n++)
            {
                var m = mono[n];
                for (var c = 0; c < ch; c++)
                    if (c != lfeIndex && gains[c] != 0) bus[c][n] += (float)(gains[c] * m);
                if (lfeIndex >= 0) { lp = (1 - alpha) * m + alpha * lp; bus[lfeIndex][n] += (float)(lfeGain * lp); }
            }
        }

        var inter = new float[outLen * ch];
        for (var n = 0; n < outLen; n++)
            for (var c = 0; c < ch; c++)
                inter[n * ch + c] = bus[c][n];

        NormalizePeak(inter, 0.97f);
        return (inter, ch, Sr);
    }

    /// <summary>Schreibt einen interleaved Stereo-/Mehrkanal-Puffer als 32-bit-Float-WAV (für Vorschau/2-Kanal).</summary>
    public static void WriteWav(string path, float[] interleaved, int channels, int sampleRate)
    {
        using var w = new WaveFileWriter(path, WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels));
        w.WriteSamples(interleaved, 0, interleaved.Length);
    }

    // Speaker-Positions-Bits (WAVEFORMATEXTENSIBLE dwChannelMask).
    private const int FL = 0x1, FR = 0x2, FC = 0x4, LFEbit = 0x8, BL = 0x10, BR = 0x20,
        SLbit = 0x200, SRbit = 0x400, TFL = 0x1000, TFR = 0x4000, TBL = 0x8000, TBR = 0x20000;

    /// <summary>Kanalmaske passend zur Kanal-Reihenfolge des jeweiligen Layouts (aufsteigende Bit-Ordnung).</summary>
    public static int ChannelMask(SpatialLayout layout) => layout switch
    {
        SpatialLayout.Surround51 => FL | FR | FC | LFEbit | SLbit | SRbit,
        SpatialLayout.Surround71 => FL | FR | FC | LFEbit | BL | BR | SLbit | SRbit,
        _ => FL | FR | FC | LFEbit | BL | BR | SLbit | SRbit | TFL | TFR | TBL | TBR
    };

    /// <summary>
    /// Schreibt einen interleaved Mehrkanal-Puffer als <b>WAVE_FORMAT_EXTENSIBLE</b> 16-bit-PCM
    /// mit korrekter <paramref name="channelMask"/> — so erkennen Windows-Player, VLC und DAWs
    /// die Lautsprecherzuordnung richtig (kein „nacktes" Float, das falsch interpretiert wird).
    /// </summary>
    public static void WriteSurroundWav(string path, float[] interleaved, int channels, int sampleRate, int channelMask)
    {
        const short bits = 16;
        const int bytesPerSample = bits / 8;
        var blockAlign = channels * bytesPerSample;
        var byteRate = sampleRate * blockAlign;
        var dataLen = interleaved.Length * bytesPerSample;

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var w = new BinaryWriter(fs);

        void Tag(string s) { foreach (var c in s) w.Write((byte)c); }

        Tag("RIFF");
        w.Write(4 + (8 + 40) + (8 + dataLen)); // RIFF chunk size
        Tag("WAVE");

        Tag("fmt ");
        w.Write(40);                            // fmt chunk size (extensible)
        w.Write(unchecked((short)0xFFFE));      // WAVE_FORMAT_EXTENSIBLE
        w.Write((short)channels);
        w.Write(sampleRate);
        w.Write(byteRate);
        w.Write((short)blockAlign);
        w.Write(bits);
        w.Write((short)22);                     // cbSize
        w.Write(bits);                          // wValidBitsPerSample
        w.Write(channelMask);
        // SubFormat-GUID: KSDATAFORMAT_SUBTYPE_PCM (00000001-0000-0010-8000-00AA00389B71)
        w.Write(new byte[]
        {
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00,
            0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71
        });

        Tag("data");
        w.Write(dataLen);
        foreach (var f in interleaved)
            w.Write((short)Math.Round(Math.Clamp(f, -1f, 1f) * 32767f));
    }

    public static string ChannelLabel(SpatialLayout layout) => layout switch
    {
        SpatialLayout.Surround51 => "5.1 (6 Kanäle)",
        SpatialLayout.Surround71 => "7.1 (8 Kanäle)",
        _ => "7.1.4 Atmos-Bett (12 Kanäle)"
    };

    // -------- Helfer --------

    private static ((double Az, double El)[] Spk, int LfeIndex) Speakers(SpatialLayout layout) => layout switch
    {
        SpatialLayout.Surround51 =>
            (new[] { (-30.0, 0.0), (30, 0), (0, 0), (0, 0), (-110, 0), (110, 0) }, 3),
        SpatialLayout.Surround71 =>
            (new[] { (-30.0, 0.0), (30, 0), (0, 0), (0, 0), (-150, 0), (150, 0), (-90, 0), (90, 0) }, 3),
        _ => // Atmos 7.1.4: 7.1-Bett + 4 Höhenkanäle (~40° Elevation)
            (new[]
            {
                (-30.0, 0.0), (30, 0), (0, 0), (0, 0), (-150, 0), (150, 0), (-90, 0), (90, 0),
                (-45, 40.0), (45, 40), (-135, 40), (135, 40)
            }, 3)
    };

    private static (double X, double Y, double Z) Unit(double azDeg, double elDeg)
    {
        var az = azDeg * Math.PI / 180.0;
        var el = elDeg * Math.PI / 180.0;
        return (Math.Sin(az) * Math.Cos(el), Math.Cos(az) * Math.Cos(el), Math.Sin(el));
    }

    private static float[][] LoadMonos(IReadOnlyList<SpatialSource> sources, out int maxLen)
    {
        var monos = new float[sources.Count][];
        maxLen = 0;
        for (var i = 0; i < sources.Count; i++)
        {
            try
            {
                var (st, sr) = AudioProcessingHelper.ReadStereo(sources[i].FilePath);
                if (sr != Sr) st = AudioProcessingHelper.Resample(st, sr, Sr);
                var frames = st.Length / 2;
                var mono = new float[frames];
                for (var n = 0; n < frames; n++) mono[n] = (st[n * 2] + st[n * 2 + 1]) * 0.5f;
                monos[i] = mono;
                if (frames > maxLen) maxLen = frames;
            }
            catch { monos[i] = []; }
        }
        return monos;
    }

    private static void NormalizePeak(float[] buffer, float ceiling)
    {
        var peak = 0f;
        for (var i = 0; i < buffer.Length; i++) peak = Math.Max(peak, Math.Abs(buffer[i]));
        if (peak <= ceiling || peak < 1e-9f) return;
        var gain = ceiling / peak;
        for (var i = 0; i < buffer.Length; i++) buffer[i] *= gain;
    }
}
