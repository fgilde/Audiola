using System.Reflection;

namespace Audiola.Services;

/// <summary>
/// Die laufende Programmversion — eine Quelle für Fenstertitel und „Über Audiola“.
///
/// Genommen wird die <see cref="AssemblyInformationalVersionAttribute"/> der Anwendung: Der
/// Release-Workflow setzt sie über <c>-p:Version=…</c>, sie trägt anders als die
/// Assembly-Version auch ein Vorab-Kennzeichen (z. B. <c>1.3.0-pre.1</c>). Der angehängte
/// Git-Stand nach dem <c>+</c> gehört nicht in die Oberfläche und fällt weg.
/// </summary>
public static class AppVersion
{
    /// <summary>Reine Versionsnummer, z. B. „1.2.6“.</summary>
    public static string Number { get; } = Resolve();

    /// <summary>True, wenn ohne gesetzte Version gebaut wurde (Entwicklungsbuild).</summary>
    public static bool IsDevBuild => Number == "1.0.0";

    /// <summary>Für den Fenstertitel: „1.2.6“ bzw. „dev“ im Entwicklungsbuild.</summary>
    public static string Short => IsDevBuild ? "dev" : Number;

    /// <summary>Für „Über Audiola“: „Version 1.2.6“ bzw. mit Hinweis im Entwicklungsbuild.</summary>
    public static string Display => IsDevBuild ? $"Version {Number} · Entwicklungsbuild" : $"Version {Number}";

    private static string Resolve()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+');
            var trimmed = (plus > 0 ? informational[..plus] : informational).Trim();
            if (trimmed.Length > 0) return trimmed;
        }

        var version = assembly.GetName().Version;
        return version is null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
