using Audiola.Dsp;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Audiola.Models;

public enum EqBandType
{
    LowShelf,
    Peaking,
    HighShelf
}

/// <summary>Ein EQ-Band: Typ, Frequenz, Gain, Güte (Q) + Anzeigefarbe.</summary>
public sealed partial class EqBand : ObservableObject
{
    [ObservableProperty] private double _frequency;
    [ObservableProperty] private double _gainDb;
    [ObservableProperty] private double _q = 1.0;

    public EqBandType Type { get; init; }
    public string ColorHex { get; init; } = "#5B8CFF";

    /// <summary>Erzeugt den passenden Biquad für dieses Band.</summary>
    public Biquad CreateFilter(int sampleRate) => Type switch
    {
        EqBandType.LowShelf => Biquad.LowShelf(sampleRate, Frequency, 0.707, GainDb),
        EqBandType.HighShelf => Biquad.HighShelf(sampleRate, Frequency, 0.707, GainDb),
        _ => Biquad.Peaking(sampleRate, Frequency, Q, GainDb)
    };
}
