using System.IO;

namespace Audiola.Services;

/// <summary>Persistierte Anwendungseinstellungen.</summary>
public sealed class AppSettings
{
    /// <summary>
    /// Pfad zur Python-Executable, in der Demucs installiert ist
    /// (z. B. "python", "py" oder ein venv-Pfad).
    /// </summary>
    public string PythonPath { get; set; } = "python";

    /// <summary>Demucs-Modellname (htdemucs ist Standard und gut ausbalanciert).</summary>
    public string DemucsModel { get; set; } = "htdemucs";

    /// <summary>Ausgabeverzeichnis fuer extrahierte Stems.</summary>
    public string OutputDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Audiola", "Stems");

    /// <summary>"Light", "Dark" oder "System".</summary>
    public string Theme { get; set; } = "System";

    /// <summary>Zuletzt geöffnete Dateien (neueste zuerst).</summary>
    public List<string> RecentFiles { get; set; } = [];

    /// <summary>Zuletzt geöffnete/gespeicherte Projekte (.audiola, neueste zuerst).</summary>
    public List<string> RecentProjects { get; set; } = [];

    /// <summary>Vom Benutzer gespeicherte Mastering-Profile.</summary>
    public List<Audiola.Models.SavedMasteringProfile> UserMasteringProfiles { get; set; } = [];

    /// <summary>ElevenLabs API-Key für den Voice-Changer (Speech-to-Speech).</summary>
    public string ElevenLabsApiKey { get; set; } = "";

    /// <summary>ElevenLabs Ziel-Voice-ID (eigene/lizenzierte Stimme).</summary>
    public string ElevenLabsVoiceId { get; set; } = "";

    /// <summary>ElevenLabs Speech-to-Speech-Modell.</summary>
    public string ElevenLabsModelId { get; set; } = "eleven_multilingual_sts_v2";
}
