using System.Text.Json;
using Audiola.Dsp;
using NAudio.Wave;

namespace Audiola.Services;

/// <summary>Eine Referenz-Note der Gesangsmelodie (für Notenband + Scoring).</summary>
public readonly record struct MelodyNote(double Start, double End, double Midi);

/// <summary>
/// Extrahiert die Gesangsmelodie einer Audiodatei offline: Datei → Mono → Pitch-Verlauf
/// (<see cref="PitchDetector.Track"/>) → zusammengefasste Noten-Segmente. Auf einer isolierten
/// Gesangsspur (Stem) präzise; auf einem vollen Mix eine brauchbare Schätzung.
/// </summary>
public static class MelodyExtractor
{
    /// <summary>Liest die Datei und liefert die Melodie als Notenliste (leer, wenn nichts Tonales gefunden).</summary>
    public static IReadOnlyList<MelodyNote> ExtractFromFile(string audioPath)
    {
        using var reader = new AudioFileReader(audioPath);
        var channels = reader.WaveFormat.Channels;
        var rate = reader.WaveFormat.SampleRate;

        // Mono-Summe einlesen.
        var mono = new List<float>(capacity: (int)Math.Min(int.MaxValue / 8, reader.Length / 4 / Math.Max(1, channels)));
        var buf = new float[rate * channels]; // 1 s pro Block
        int read;
        while ((read = reader.Read(buf, 0, buf.Length)) > 0)
        {
            for (var i = 0; i + channels <= read; i += channels)
            {
                float m = 0;
                for (var c = 0; c < channels; c++) m += buf[i + c];
                mono.Add(m / channels);
            }
        }

        var track = PitchDetector.Track(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(mono), rate);
        return Segment(track);
    }

    /// <summary>Fasst den rohen Pitch-Verlauf zu Noten zusammen (gleiche Höhe ±0,7 Halbton, ≥ 0,15 s).</summary>
    public static IReadOnlyList<MelodyNote> Segment(IReadOnlyList<PitchPoint> track)
    {
        const double tolerance = 0.7;   // Halbtöne, innerhalb derer ein Ton als „gehalten" gilt
        const double minDur = 0.15;     // kürzere Schnipsel sind meist Erkennungsrauschen
        const double maxGap = 0.12;     // kurze Aussetzer innerhalb einer Note überbrücken

        var notes = new List<MelodyNote>();
        double segStart = -1, segEnd = 0, sum = 0;
        int count = 0;

        void Flush()
        {
            if (count > 0 && segEnd - segStart >= minDur)
                notes.Add(new MelodyNote(segStart, segEnd, Math.Round(sum / count * 2) / 2));
            segStart = -1; sum = 0; count = 0;
        }

        foreach (var p in track)
        {
            if (p.Hz <= 0)
            {
                if (segStart >= 0 && p.TimeSeconds - segEnd > maxGap) Flush();
                continue;
            }
            var midi = PitchDetector.HzToMidi(p.Hz);
            if (segStart < 0)
            {
                segStart = p.TimeSeconds; segEnd = p.TimeSeconds; sum = midi; count = 1;
            }
            else if (Math.Abs(midi - sum / count) <= tolerance && p.TimeSeconds - segEnd <= maxGap + 0.05)
            {
                segEnd = p.TimeSeconds; sum += midi; count++;
            }
            else
            {
                Flush();
                segStart = p.TimeSeconds; segEnd = p.TimeSeconds; sum = midi; count = 1;
            }
        }
        Flush();
        return notes;
    }

    // ---- Cache-Serialisierung (SongCache.MelodyJson) ----

    public static string ToJson(IReadOnlyList<MelodyNote> notes) =>
        JsonSerializer.Serialize(notes.Select(n => new[] { Math.Round(n.Start, 3), Math.Round(n.End, 3), n.Midi }));

    public static IReadOnlyList<MelodyNote> FromJson(string? json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return [];
            var raw = JsonSerializer.Deserialize<double[][]>(json!) ?? [];
            return raw.Where(a => a.Length == 3).Select(a => new MelodyNote(a[0], a[1], a[2])).ToList();
        }
        catch { return []; }
    }
}
