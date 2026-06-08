namespace Audiola.Web.Services;

/// <summary>Gemeinsamer App-Zustand (wie SessionState im WPF): die aktuell geöffnete Datei.</summary>
public sealed class AppState
{
    public byte[]? FileBytes { get; private set; }
    public string? FileName { get; private set; }
    public bool HasFile => FileBytes is { Length: > 0 };

    public event Action? Changed;

    public void SetFile(byte[] bytes, string name)
    {
        FileBytes = bytes;
        FileName = name;
        Changed?.Invoke();
    }
}
