using Audiola.Models;

namespace Audiola.Services;

/// <summary>Persistenter, projektunabhängiger Speicher für Stimmen-Profile.</summary>
public interface IVoiceProfileStore
{
    IReadOnlyList<VoiceProfile> Profiles { get; }

    event EventHandler? Changed;

    /// <summary>Fügt ein Profil hinzu; kopiert Referenz-Samples in den Stimmen-Ordner.</summary>
    VoiceProfile Add(VoiceProfile profile, IEnumerable<string> sampleSources);

    void Remove(string id);

    VoiceProfile? Get(string id);

    /// <summary>Verzeichnis, in dem Profile + Samples liegen.</summary>
    string StoreDirectory { get; }
}
