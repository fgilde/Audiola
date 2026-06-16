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

    /// <summary>Demucs-Modellname (htdemucs_ft = fine-tuned, spürbar besser bei gleicher Geschwindigkeit).</summary>
    public string DemucsModel { get; set; } = "htdemucs_ft";

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

    // ---- Lokale Voice-Engine (VoiceBox-artig, Python-Sidecar) ----

    /// <summary>Rechen-Gerät für lokale Modelle: "auto", "cuda", "cpu" oder "directml".</summary>
    public string VoiceDevice { get; set; } = "auto";

    /// <summary>Verzeichnis für heruntergeladene lokale Modelle.</summary>
    public string VoiceModelsDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Audiola", "voice-models");

    /// <summary>Standard-TTS-Modell für die lokale Sprachsynthese.</summary>
    public string LocalTtsModel { get; set; } = "kokoro";

    /// <summary>Whisper-Modellgröße für die Transkription: tiny/base/small/medium/large-v3/turbo.</summary>
    public string WhisperModel { get; set; } = "base";
}
