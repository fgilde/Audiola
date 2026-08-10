using CommunityToolkit.Mvvm.ComponentModel;

namespace Audiola.ViewModels;

/// <summary>
/// Ein Clip auf einer Spur: ein Ausschnitt einer Quelldatei
/// (<see cref="SourceStartSeconds"/> … +<see cref="LengthSeconds"/>), platziert bei
/// <see cref="TimelineOffsetSeconds"/>. Die Quelle ist clip-eigen, sodass eine Spur
/// Clips aus verschiedenen Dateien enthalten kann. Lautstärke/Pan/Mute/Solo gehören zur Spur.
/// </summary>
public sealed partial class ClipViewModel : ObservableObject
{
    public required StemTrackViewModel Track { get; init; }

    /// <summary>Pfad der Quelldatei dieses Clips.</summary>
    public required string SourcePath { get; init; }

    /// <summary>Gesamtlänge der Quelldatei (für Trim/Slice).</summary>
    public double SourceTotalSeconds { get; init; }

    /// <summary>Volle Wellenform-Peaks der Quelldatei (für das Neuschneiden beim Trimmen).</summary>
    public float[] SourcePeaks { get; init; } = [];

    [ObservableProperty] private double _timelineOffsetSeconds;
    [ObservableProperty] private double _sourceStartSeconds;
    [ObservableProperty] private double _lengthSeconds;
    [ObservableProperty] private float[] _peaks = [];
    [ObservableProperty] private bool _isSelected;

    /// <summary>Clip-Lautstärke in dB (0 = unverändert).</summary>
    [ObservableProperty] private double _gainDb;
    /// <summary>Ein-/Ausblendlänge in Sekunden.</summary>
    [ObservableProperty] private double _fadeInSeconds;
    [ObservableProperty] private double _fadeOutSeconds;

    public string AccentColor => Track.AccentColor;
    public string DisplayName => Track.DisplayName;

    public double EndSeconds => TimelineOffsetSeconds + LengthSeconds;
}
