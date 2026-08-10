namespace Audiola.Services;

/// <summary>Nimmt Audio von einem Eingabegerät (Mikrofon) in eine WAV-Datei auf.</summary>
public interface IAudioRecorder
{
    bool IsRecording { get; }

    /// <summary>Startet die Aufnahme (Gerät = NAudio-DeviceNumber) und liefert den WAV-Zielpfad.</summary>
    string Start(int deviceNumber = 0);

    /// <summary>Stoppt die Aufnahme und liefert (nach dem Flush) den fertigen WAV-Pfad.</summary>
    Task<string> StopAsync();
}
