using System.IO;

namespace Audiola.Services;

/// <summary>
/// Zentrale Temp-Pfade der App: <c>%Temp%/Audiola/&lt;kategorie&gt;</c>. Ersetzt die überall
/// duplizierte Kombination aus Path.Combine + CreateDirectory + Guid-Dateiname und gibt
/// dem Aufräumen einen einzigen Wurzelordner.
/// </summary>
public static class TempDir
{
    /// <summary>Wurzel aller Audiola-Temp-Dateien.</summary>
    public static string Root => Path.Combine(Path.GetTempPath(), "Audiola");

    /// <summary>Liefert (und erstellt) den Temp-Ordner einer Kategorie, z. B. "rec", "master".</summary>
    public static string Category(string category)
    {
        var dir = Path.Combine(Root, category);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Eindeutiger Dateipfad in einer Kategorie, z. B. <c>File("vc", ".wav")</c>.</summary>
    public static string File(string category, string extension, string? prefix = null)
        => Path.Combine(Category(category), $"{prefix}{(prefix is null ? "" : "_")}{Guid.NewGuid():N}{extension}");

    /// <summary>Eindeutiger Unterordner in einer Kategorie (für Entpacken/Batches).</summary>
    public static string Folder(string category)
    {
        var dir = Path.Combine(Category(category), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Löscht alte Temp-Dateien (älter als <paramref name="olderThan"/>) unterhalb der Wurzel. Fehler werden ignoriert.</summary>
    public static void Cleanup(TimeSpan olderThan)
    {
        try
        {
            if (!Directory.Exists(Root)) return;
            var cutoff = DateTime.UtcNow - olderThan;
            foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
            {
                try
                {
                    if (System.IO.File.GetLastWriteTimeUtc(file) < cutoff)
                        System.IO.File.Delete(file);
                }
                catch { /* gesperrt/in Benutzung */ }
            }
        }
        catch { /* Aufräumen ist Best-Effort */ }
    }
}
