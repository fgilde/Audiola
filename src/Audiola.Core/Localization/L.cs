using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Audiola;

/// <summary>
/// Übersetzung der Oberfläche. Der deutsche Originaltext ist der Schlüssel; für jede
/// weitere Sprache liegt eine eingebettete JSON-Datei (Localization/&lt;code&gt;.json,
/// deutsch → Übersetzung) bei. Fehlt ein Eintrag, bleibt der deutsche Text stehen.
/// Die Sprache wird einmal beim Start gesetzt; ein Wechsel greift nach dem Neustart.
/// </summary>
public static class L
{
    /// <summary>Wählbare Sprachen (Code, Anzeigename in der jeweiligen Sprache).</summary>
    public static IReadOnlyList<(string Code, string Name)> Languages { get; } =
        [("de", "Deutsch"), ("en", "English"), ("fr", "Français")];

    private static Dictionary<string, string>? _map;

    /// <summary>Aktive Sprache ("de", "en", "fr").</summary>
    public static string Current { get; private set; } = "de";

    /// <summary>Setzt die Sprache; leer/unbekannt = Systemsprache, sonst Englisch.</summary>
    public static void Use(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (!Languages.Any(l => l.Code == code)) code = "en";
        Current = code;
        _map = code == "de" ? null : Load(code);
        var culture = CultureInfo.GetCultureInfo(code);
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    /// <summary>Übersetzt einen deutschen Oberflächentext.</summary>
    public static string T(string de) => _map is not null && _map.TryGetValue(de, out var s) ? s : de;

    /// <summary>Übersetzt eine deutsche Formatvorlage ({0}, {1} …) und setzt die Werte ein.</summary>
    public static string F(string de, params object?[] args) => string.Format(T(de), args);

    private static Dictionary<string, string>? Load(string code)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Audiola.Localization.{code}.json");
        return stream is null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
    }
}
