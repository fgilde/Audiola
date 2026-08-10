using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using NAudio.Wave;

namespace Audiola.Services.Audio;

/// <summary>
/// Öffnet Audiodateien auf allen Plattformen.
///
/// NAudios <see cref="AudioFileReader"/> liest MP3, M4A und FLAC nur unter Windows (Media
/// Foundation bzw. ACM). Auf macOS und Linux wandelt diese Klasse solche Dateien einmalig per
/// FFmpeg in eine temporäre WAV um und liest die — der Rest der App arbeitet unverändert mit
/// einem <see cref="AudioFileReader"/> weiter (Volume, CurrentTime, TotalTime, Read).
///
/// Die umgewandelten Dateien werden zwischengespeichert, damit dieselbe Quelle nicht bei jedem
/// Öffnen erneut dekodiert wird, und liegen im Temp-Verzeichnis der App.
/// </summary>
public static class PortableAudioFile
{
    /// <summary>Formate, die NAudio auf jeder Plattform ohne Hilfe liest.</summary>
    private static readonly string[] NativeExtensions = [".wav", ".aiff", ".aif"];

    /// <summary>Quelle (Pfad + Änderungszeit + Größe) → schon dekodierte WAV.</summary>
    private static readonly ConcurrentDictionary<string, string> DecodedCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Öffnet die Datei als <see cref="AudioFileReader"/> — bei Bedarf über eine
    /// FFmpeg-Zwischenstufe. Der Aufrufer gibt den Reader wie gewohnt frei.
    /// </summary>
    public static AudioFileReader Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new AudioFileReader(ReadablePath(path));
    }

    /// <summary>
    /// Liefert einen Pfad, den NAudio auf dieser Plattform sicher lesen kann — entweder die
    /// Originaldatei oder eine dekodierte WAV daneben. Nützlich für Dienste, die den Pfad
    /// selbst weiterverwenden (Wellenform, Analyse).
    /// </summary>
    public static string ReadablePath(string path)
    {
        if (OperatingSystem.IsWindows()) return path;

        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (NativeExtensions.Contains(extension)) return path;
        if (!File.Exists(path)) return path;   // Fehler soll der Reader melden, nicht wir

        var key = CacheKey(path);
        if (DecodedCache.TryGetValue(key, out var cached) && File.Exists(cached)) return cached;

        var decoded = DecodeToWav(path);
        DecodedCache[key] = decoded;
        return decoded;
    }

    private static string CacheKey(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return $"{info.FullName}|{info.LastWriteTimeUtc.Ticks}|{info.Length}";
        }
        catch { return path; }
    }

    private static string DecodeToWav(string path)
    {
        var target = TempDir.File("decoded", ".wav", Path.GetFileNameWithoutExtension(path));
        var ffmpeg = FfmpegExecutableLocator.Find();

        var startInfo = new ProcessStartInfo(ffmpeg)
        {
            // 32-Bit-Float, Originalrate/-kanäle behalten: verlustfrei für die weitere Verarbeitung.
            ArgumentList = { "-hide_banner", "-loglevel", "error", "-y", "-i", path, "-c:a", "pcm_f32le", target },
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("FFmpeg konnte nicht gestartet werden.");
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0 || !File.Exists(target))
            throw new InvalidOperationException(
                $"„{Path.GetFileName(path)}“ konnte auf dieser Plattform nicht dekodiert werden. {error}".Trim());

        return target;
    }
}
