using System.Diagnostics;
using System.Text;

namespace Audiola.Services;

/// <summary>Ergebnis eines Prozesslaufs: Exit-Code plus gepufferte Ausgaben.</summary>
public sealed record ProcessResult(int ExitCode, string Stdout, string Stderr)
{
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Zentraler Prozess-Runner für alle Sidecar-/Tool-Aufrufe (Python, pip, ffmpeg …).
/// Ersetzt die zuvor sechsfach duplizierten ProcessStartInfo-Implementierungen.
///
/// stderr wird IMMER zeilenweise gelesen (Live-Fortschritt an <c>progress</c>) und gepuffert.
/// stdout hat zwei Modi:
///  - <see cref="StdoutMode.Buffer"/>: komplett via ReadToEndAsync — ohne Zeilen-Race,
///    für große JSON-Ausgaben in einer Zeile (Transkription, Modell-Listen).
///  - <see cref="StdoutMode.StreamLines"/>: zeilenweise an <c>progress</c> UND gepuffert —
///    für Tools, die ihren Fortschritt über stdout melden (pip, Demucs).
/// Bei Abbruch wird der gesamte Prozessbaum beendet.
/// </summary>
public static class ProcessRunner
{
    public enum StdoutMode { Buffer, StreamLines }

    public static async Task<ProcessResult> RunAsync(
        string exe,
        IReadOnlyList<string> args,
        IProgress<string>? progress = null,
        CancellationToken ct = default,
        StdoutMode stdoutMode = StdoutMode.Buffer,
        string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (!string.IsNullOrEmpty(workingDirectory)) psi.WorkingDirectory = workingDirectory;
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stderr = new StringBuilder();
        var stdout = new StringBuilder();

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            stderr.AppendLine(e.Data);
            progress?.Report(e.Data);
        };

        if (stdoutMode == StdoutMode.StreamLines)
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                stdout.AppendLine(e.Data);
                progress?.Report(e.Data);
            };

        process.Start();
        process.BeginErrorReadLine();

        Task<string>? stdoutTask = null;
        if (stdoutMode == StdoutMode.Buffer)
            stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        else
            process.BeginOutputReadLine();

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* schon beendet */ }
            throw;
        }

        if (stdoutTask is not null)
            stdout.Append(await stdoutTask);   // sicherstellen, dass der gesamte stdout gelesen wurde

        return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }
}
