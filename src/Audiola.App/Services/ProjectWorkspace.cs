using Audiola.ViewModels;

namespace Audiola.Services;

/// <summary>
/// Orchestriert Speichern/Laden eines Projekts über alle beteiligten ViewModels
/// (Timeline, Equalizer, Mastering) und pflegt die Liste „Letzte Projekte".
/// </summary>
public sealed class ProjectWorkspace
{
    private readonly IProjectService _project;
    private readonly ISettingsService _settings;
    private readonly TimelineViewModel _timeline;
    private readonly EqualizerViewModel _eq;
    private readonly MasteringViewModel _mastering;
    private readonly SpatialAudioViewModel _spatial;
    private readonly SongMetadata _metadata;

    public ProjectWorkspace(IProjectService project, ISettingsService settings,
        TimelineViewModel timeline, EqualizerViewModel eq, MasteringViewModel mastering,
        SpatialAudioViewModel spatial, SongMetadata metadata)
    {
        _project = project;
        _settings = settings;
        _timeline = timeline;
        _eq = eq;
        _mastering = mastering;
        _spatial = spatial;
        _metadata = metadata;
    }

    public event EventHandler? RecentChanged;

    public string? CurrentPath => _timeline.CurrentProjectPath;
    public bool IsDirty => _timeline.IsDirty;
    public bool HasContent => _timeline.Tracks.Count > 0;
    public IReadOnlyList<string> RecentProjects => _settings.Current.RecentProjects;

    public async Task SaveAsync(string path)
    {
        var dto = _timeline.BuildProjectDto();
        dto.Eq = _eq.ExportBands();
        var (ms, profile) = _mastering.ExportSettings();
        dto.Mastering = ms;
        dto.MasteringProfile = profile;
        dto.Spatial = _spatial.ExportSpatial();
        dto.Metadata = _metadata.ToMetadata();

        await _project.SaveAsync(path, dto);

        _timeline.CurrentProjectPath = path;
        _timeline.IsDirty = false;
        AddRecent(path);
    }

    public async Task OpenAsync(string path)
    {
        var dto = await _project.LoadAsync(path);
        await _timeline.ApplyProjectDtoAsync(dto);   // setzt IsDirty=false + leert Undo-Historie
        _eq.ImportBands(dto.Eq);
        _mastering.ImportSettings(dto.Mastering, dto.MasteringProfile);
        _spatial.ImportSpatial(dto.Spatial);
        _metadata.Clear();
        _metadata.Apply(dto.Metadata);

        _timeline.CurrentProjectPath = path;
        AddRecent(path);
    }

    /// <summary>Entfernt ein Projekt aus der „Letzte Projekte"-Liste.</summary>
    public void RemoveRecent(string path)
    {
        var n = _settings.Current.RecentProjects.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        if (n > 0) { _settings.Save(); RecentChanged?.Invoke(this, EventArgs.Empty); }
    }

    private void AddRecent(string path)
    {
        var list = _settings.Current.RecentProjects;
        list.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, path);
        while (list.Count > 10) list.RemoveAt(list.Count - 1);
        _settings.Save();
        RecentChanged?.Invoke(this, EventArgs.Empty);
    }
}
