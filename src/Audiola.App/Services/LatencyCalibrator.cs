using System.IO;
using Audiola.Services;
using NAudio.Wave;

namespace Audiola.Services;

/// <summary>
/// Misst den Aufnahme-Round-Trip-Versatz: spielt eine Reihe kurzer Klicks ab, nimmt gleichzeitig
/// auf und bestimmt den zeitlichen Abstand zwischen erwartetem und aufgenommenem Klick. Das Ergebnis
/// (Median über mehrere Klicks) kompensiert die Aufnahme, damit der Gesang im Takt sitzt.
///
/// Hinweis: Für die Messung MÜSSEN die Klicks vom Mikrofon hörbar sein — also OHNE Kopfhörer bzw.
/// mit offenem Lautsprecher. Danach wird zum Einsingen wieder der Kopfhörer genutzt.
/// </summary>
public sealed class LatencyCalibrator
{
    private const int Rate = 44100;
    private static readonly double[] ClickTimes = [0.6, 1.1, 1.6, 2.1, 2.6];
    private const double TotalSeconds = 3.2;

    /// <summary>Misst den Versatz in Millisekunden. Wirft, wenn keine Klicks erkannt wurden.</summary>
    public async Task<double> MeasureMsAsync(int deviceNumber = 0, CancellationToken ct = default)
    {
        var clickPath = WriteClickSignal();
        var recorded = new List<float>(capacity: (int)(TotalSeconds * Rate) + Rate);

        using var mic = new WaveInEvent { DeviceNumber = deviceNumber, WaveFormat = new WaveFormat(Rate, 16, 1), BufferMilliseconds = 30 };
        mic.DataAvailable += (_, e) =>
        {
            int n = e.BytesRecorded / 2;
            for (int i = 0; i < n; i++)
            {
                short s = (short)(e.Buffer[i * 2] | (e.Buffer[i * 2 + 1] << 8));
                recorded.Add(s / 32768f);
            }
        };

        using var reader = new AudioFileReader(clickPath);
        using var outp = new WaveOutEvent();
        outp.Init(reader);
        var done = new TaskCompletionSource();
        outp.PlaybackStopped += (_, _) => done.TrySetResult();

        // So gleichzeitig wie möglich starten — der kleine Rest-Versatz gehört zur gemessenen Latenz.
        mic.StartRecording();
        outp.Play();
        await Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromSeconds(TotalSeconds + 1), ct));
        try { mic.StopRecording(); } catch { }
        try { File.Delete(clickPath); } catch { }

        var rec = recorded.ToArray();
        var offsets = new List<double>();
        foreach (var t in ClickTimes)
        {
            int from = (int)(t * Rate);
            int to = Math.Min(rec.Length, (int)((t + 0.35) * Rate)); // Latenz bis ~350 ms suchen
            float max = 0f; int maxIdx = -1;
            for (int i = from; i < to; i++)
            {
                float a = Math.Abs(rec[i]);
                if (a > max) { max = a; maxIdx = i; }
            }
            if (maxIdx >= 0 && max > 0.04f)
                offsets.Add((maxIdx - t * Rate) / (double)Rate);
        }

        if (offsets.Count == 0)
            throw new InvalidOperationException(
                "Es wurde kein Klick erkannt. Bitte ohne Kopfhörer kalibrieren, das Mikrofon aktivieren " +
                "und die Lautsprecher-Lautstärke etwas erhöhen.");

        offsets.Sort();
        return offsets[offsets.Count / 2] * 1000.0; // Median in ms
    }

    private static string WriteClickSignal()
    {
        var sig = new float[(int)(TotalSeconds * Rate)];
        int clickLen = (int)(0.003 * Rate); // 3 ms Impuls
        foreach (var t in ClickTimes)
        {
            int start = (int)(t * Rate);
            for (int i = 0; i < clickLen && start + i < sig.Length; i++)
                sig[start + i] = 0.9f * (1f - (float)i / clickLen); // kurzer, abfallender Impuls
        }
        var path = TempDir.File("calib", ".wav", "click");
        AudioExporter.Export(sig, Rate, 1, path);
        return path;
    }
}
