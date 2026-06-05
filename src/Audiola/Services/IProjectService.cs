using Audiola.Models;

namespace Audiola.Services;

/// <summary>Speichert/lädt Audiola-Projekte (.audiola = ZIP mit Manifest + Medien).</summary>
public interface IProjectService
{
    /// <summary>Schreibt das Projekt samt aller referenzierten Audiodateien in eine .audiola-Datei.</summary>
    Task SaveAsync(string path, ProjectDto project);

    /// <summary>
    /// Lädt ein Projekt: entpackt die Medien in einen Arbeitsordner und liefert das Manifest,
    /// dessen Clip-Pfade auf die entpackten Dateien zeigen.
    /// </summary>
    Task<ProjectDto> LoadAsync(string path);
}
