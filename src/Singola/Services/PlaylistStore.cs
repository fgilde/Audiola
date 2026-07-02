using System.IO;
using System.Text.Json;

namespace Singola.Services;

/// <summary>Eine Karaoke-Playlist: Name + Song-Pfade (Reihenfolge = Abspielreihenfolge).</summary>
public sealed class Playlist
{
    public string Name { get; set; } = "Neue Playlist";
    public List<string> SongPaths { get; set; } = [];
    public override string ToString() => $"{Name} ({SongPaths.Count})";
}

/// <summary>Playlisten-Ablage als JSON unter %LOCALAPPDATA%\Audiola\singola-playlists.json.</summary>
public static class PlaylistStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Audiola", "singola-playlists.json");

    public static List<Playlist> Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<List<Playlist>>(File.ReadAllText(FilePath)) ?? [];
        }
        catch { }
        return [];
    }

    public static void Save(IEnumerable<Playlist> lists)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(lists.ToList(),
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
