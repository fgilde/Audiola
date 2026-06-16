using System.IO;
using System.Text.Json;
using Audiola.Models;

namespace Audiola.Services;

/// <summary>
/// Speichert Stimmen-Profile als <c>profiles.json</c> + kopierte Referenz-Samples unter
/// <c>%LocalAppData%/Audiola/voices/</c> — unabhängig von einzelnen Audioprojekten.
/// </summary>
public sealed class VoiceProfileStore : IVoiceProfileStore
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly string _dir;
    private readonly string _manifest;
    private readonly List<VoiceProfile> _profiles = [];

    public VoiceProfileStore()
    {
        _dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Audiola", "voices");
        Directory.CreateDirectory(_dir);
        _manifest = Path.Combine(_dir, "profiles.json");
        Load();
    }

    public IReadOnlyList<VoiceProfile> Profiles => _profiles;
    public string StoreDirectory => _dir;
    public event EventHandler? Changed;

    public VoiceProfile? Get(string id) => _profiles.FirstOrDefault(p => p.Id == id);

    public VoiceProfile Add(VoiceProfile profile, IEnumerable<string> sampleSources)
    {
        var mediaDir = Path.Combine(_dir, profile.Id);
        Directory.CreateDirectory(mediaDir);

        var copied = new List<string>();
        foreach (var src in sampleSources)
        {
            if (string.IsNullOrWhiteSpace(src) || !File.Exists(src)) continue;
            var dest = Path.Combine(mediaDir, Path.GetFileName(src));
            try { File.Copy(src, dest, overwrite: true); copied.Add(dest); } catch { /* ignore */ }
        }
        if (copied.Count > 0) profile.SamplePaths = copied;

        _profiles.Insert(0, profile);
        Save();
        return profile;
    }

    public void Remove(string id)
    {
        var p = Get(id);
        if (p is null) return;
        _profiles.Remove(p);
        try
        {
            var mediaDir = Path.Combine(_dir, id);
            if (Directory.Exists(mediaDir)) Directory.Delete(mediaDir, recursive: true);
        }
        catch { /* ignore */ }
        Save();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_manifest)) return;
            var list = JsonSerializer.Deserialize<List<VoiceProfile>>(File.ReadAllText(_manifest), JsonOpts);
            if (list is not null) { _profiles.Clear(); _profiles.AddRange(list); }
        }
        catch { /* korruptes Manifest ignorieren */ }
    }

    private void Save()
    {
        try { File.WriteAllText(_manifest, JsonSerializer.Serialize(_profiles, JsonOpts)); }
        catch { /* ignore */ }
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
