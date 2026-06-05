using Audiola.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Audiola.ViewModels;

/// <summary>
/// Geteilter Zustand ueber alle Seiten hinweg: aktuell geladener Track
/// und das letzte Stem-Trennungsergebnis.
/// </summary>
public sealed partial class SessionState : ObservableObject
{
    [ObservableProperty]
    private AudioTrack? _currentTrack;

    [ObservableProperty]
    private StemSet? _currentStemSet;

    public bool HasTrack => CurrentTrack is not null;

    partial void OnCurrentTrackChanged(AudioTrack? value) => OnPropertyChanged(nameof(HasTrack));
}
