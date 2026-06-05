namespace Audiola.Services;

/// <summary>Nimmt Audio vom Standard-Eingabegerät (Mikrofon) in eine WAV-Datei auf.</summary>
public interface IAudioRecorder
{
    bool IsRecording { get; }

    /// <summary>Startet die Aufnahme und liefert den Zielpfad der WAV-Datei.</summary>
    string Start();

    /// <summary>Stoppt die Aufnahme und liefert (nach dem Flush) den fertigen WAV-Pfad.</summary>
    Task<string> StopAsync();
}
