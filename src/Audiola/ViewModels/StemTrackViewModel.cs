using System.Collections.ObjectModel;
using Audiola.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Audiola.ViewModels;

/// <summary>Observable-Huelle um einen <see cref="Stem"/> fuer die Mixer-Regler.</summary>
public sealed partial class StemTrackViewModel : ObservableObject
{
    private readonly Stem _stem;

    public StemTrackViewModel(Stem stem)
    {
        _stem = stem;
        _volume = stem.Volume;
        _pan = stem.Pan;
        _isEnabled = stem.IsEnabled;
        _isMuted = stem.IsMuted;
        _isSolo = stem.IsSolo;
        _name = stem.Kind switch
        {
            StemKind.Vocals => "Vocals",
            StemKind.Drums => "Drums",
            StemKind.Bass => "Bass",
            StemKind.Guitar => "Guitar",
            StemKind.Piano => "Piano",
            _ => "Other"
        };
    }

    public Stem Model => _stem;

    /// <summary>Eigene Akzentfarbe für generische Spuren (z. B. importierte Dateien).</summary>
    public string? CustomColorHex { get; init; }

    /// <summary>Editierbarer Spurname.</summary>
    [ObservableProperty]
    private string _name;

    /// <summary>Erzeugt eine generische Spur aus einer beliebigen Audiodatei.</summary>
    public static StemTrackViewModel ForFile(string path, string name, string colorHex)
        => new(new Stem { Kind = StemKind.Other, FilePath = path })
        {
            Name = name,
            CustomColorHex = colorHex
        };

    public string DisplayName => Name;

    /// <summary>Akzentfarbe je Stem-Typ fuer die Liste.</summary>
    public string AccentColor => CustomColorHex ?? _stem.Kind switch
    {
        StemKind.Vocals => "#FF6B6B",
        StemKind.Drums => "#FFB454",
        StemKind.Bass => "#5B8CFF",
        StemKind.Guitar => "#54D6A0",
        StemKind.Piano => "#6BD6FF",
        _ => "#9B8CFF"
    };

    /// <summary>Wellenform-Peaks des Stems (wird asynchron nachgeladen).</summary>
    [ObservableProperty]
    private float[] _peaks = [];

    /// <summary>Start-Offset des Clips auf der Timeline (Sekunden).</summary>
    [ObservableProperty]
    private double _startOffsetSeconds;

    /// <summary>Länge des Stems (Sekunden) — für die Clip-Breite.</summary>
    [ObservableProperty]
    private double _lengthSeconds;

    /// <summary>Clips dieser Spur (Timeline). Leer ⇒ ganzer Stem (z. B. in „Stems &amp; Mix“).</summary>
    public ObservableCollection<ClipViewModel> Clips { get; } = [];

    /// <summary>Roh-Spitzenpegel des aktuellen Blocks (vom Audio-Thread geschrieben).</summary>
    public float MeterPeak;

    /// <summary>Geglätteter Pegel 0..~1 für die VU-Anzeige (UI-Thread).</summary>
    [ObservableProperty]
    private double _level;

    /// <summary>True, wenn der aktuelle Auswahlbereich auf dieser Spur liegt (für das Band).</summary>
    [ObservableProperty]
    private bool _isSelectionTrack;

    /// <summary>True, wenn diese Spur die aktuell ausgewählte ist (hervorgehobene Anzeige).</summary>
    [ObservableProperty]
    private bool _isSelectedTrack;

    [ObservableProperty]
    private double _volume;

    [ObservableProperty]
    private double _pan;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private bool _isSolo;

    partial void OnVolumeChanged(double value) => _stem.Volume = (float)value;
    partial void OnPanChanged(double value) => _stem.Pan = (float)value;
    partial void OnIsEnabledChanged(bool value) => _stem.IsEnabled = value;
    partial void OnIsMutedChanged(bool value) => _stem.IsMuted = value;
    partial void OnIsSoloChanged(bool value) => _stem.IsSolo = value;
}
