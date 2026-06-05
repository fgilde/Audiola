using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Audiola.Models;

namespace Audiola.Services;

/// <summary>
/// .audiola-Projektdatei = ZIP mit <c>project.json</c> (Manifest) und einem
/// <c>media/</c>-Ordner mit allen referenzierten Audiodateien (Stems, importierte
/// Dateien, gebackene Effekt-Clips). So muss beim Wiederöffnen nichts neu extrahiert werden.
/// </summary>
public sealed class ProjectService : IProjectService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private static string ProjectsRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Audiola", "projects");

    public Task SaveAsync(string path, ProjectDto project) => Task.Run(() =>
    {
        // Eindeutige Quelldateien sammeln und relative Ziele im ZIP zuweisen.
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // abs -> media/relativ
        var i = 0;
        foreach (var t in project.Tracks)
            foreach (var c in t.Clips)
            {
                if (string.IsNullOrEmpty(c.Media) || map.ContainsKey(c.Media)) continue;
                map[c.Media] = $"media/{i++}_{Path.GetFileName(c.Media)}";
            }

        if (File.Exists(path)) File.Delete(path);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);

        foreach (var kv in map)
            if (File.Exists(kv.Key))
                zip.CreateEntryFromFile(kv.Key, kv.Value, CompressionLevel.Fastest);

        // Clip-Pfade auf relative ZIP-Pfade umschreiben (nach dem Kopieren).
        foreach (var t in project.Tracks)
            foreach (var c in t.Clips)
                if (!string.IsNullOrEmpty(c.Media) && map.TryGetValue(c.Media, out var rel))
                    c.Media = rel;

        var entry = zip.CreateEntry("project.json", CompressionLevel.Fastest);
        using var w = new StreamWriter(entry.Open());
        w.Write(JsonSerializer.Serialize(project, JsonOpts));
    });

    public Task<ProjectDto> LoadAsync(string path) => Task.Run(() =>
    {
        var workDir = Path.Combine(ProjectsRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        using var zip = ZipFile.OpenRead(path);
        var manifest = zip.GetEntry("project.json")
            ?? throw new InvalidOperationException("Keine project.json im Projekt gefunden.");

        ProjectDto dto;
        using (var reader = new StreamReader(manifest.Open()))
            dto = JsonSerializer.Deserialize<ProjectDto>(reader.ReadToEnd(), JsonOpts) ?? new ProjectDto();

        foreach (var e in zip.Entries)
        {
            if (string.IsNullOrEmpty(e.Name)) continue; // Verzeichnis-Eintrag
            if (!e.FullName.StartsWith("media/", StringComparison.OrdinalIgnoreCase)) continue;
            var dest = Path.Combine(workDir, e.FullName.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            e.ExtractToFile(dest, overwrite: true);
        }

        // Clip-Pfade auf die entpackten Dateien (absolut) umschreiben.
        foreach (var t in dto.Tracks)
            foreach (var c in t.Clips)
                if (!string.IsNullOrEmpty(c.Media))
                    c.Media = Path.Combine(workDir, c.Media.Replace('/', Path.DirectorySeparatorChar));

        return dto;
    });
}
