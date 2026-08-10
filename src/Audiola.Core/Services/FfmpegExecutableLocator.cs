using System.IO;

namespace Audiola.Services;

/// <summary>Finds the FFmpeg executable from the app bundle before falling back to PATH.</summary>
internal static class FfmpegExecutableLocator
{
    private const string ExecutableName = "ffmpeg";

    public static string Find()
    {
        var fileName = OperatingSystem.IsWindows() ? $"{ExecutableName}.exe" : ExecutableName;
        var bundledPath = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(bundledPath))
            return bundledPath;

        var path = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(path))
        {
            foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var candidate = Path.Combine(directory.Trim().Trim('"'), fileName);
                    if (File.Exists(candidate))
                        return candidate;
                }
                catch (ArgumentException)
                {
                    // Ignore malformed PATH entries and keep looking.
                }
            }
        }

        throw new InvalidOperationException(
            $"FFmpeg is required to export MP3 or AAC on this platform. " +
            $"Bundle '{fileName}' next to the application at '{bundledPath}', or install FFmpeg and add it to PATH.");
    }
}
