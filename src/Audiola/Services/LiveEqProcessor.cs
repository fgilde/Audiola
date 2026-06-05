using Audiola.Dsp;
using Audiola.Models;

namespace Audiola.Services;

/// <summary>
/// Geteilter Live-EQ, den der Player in die Wiedergabekette einhängt. Die
/// Equalizer-Seite setzt die Bänder und schaltet die Vorschau an/aus; die
/// Verarbeitung läuft auf dem Audio-Thread, Updates kommen vom UI-Thread —
/// daher per Lock + Dirty-Flag synchronisiert.
/// </summary>
public sealed class LiveEqProcessor
{
    private readonly object _gate = new();
    private IReadOnlyList<EqBand>? _bands;
    private Biquad[][] _filters = [];
    private int _sampleRate = 44100;
    private bool _enabled;
    private bool _dirty;

    public bool Enabled
    {
        get { lock (_gate) return _enabled; }
        set { lock (_gate) { _enabled = value; _dirty = true; } }
    }

    public void SetBands(IReadOnlyList<EqBand> bands)
    {
        lock (_gate) { _bands = bands; _dirty = true; }
    }

    public void MarkDirty()
    {
        lock (_gate) _dirty = true;
    }

    public void Configure(int sampleRate)
    {
        lock (_gate) { _sampleRate = sampleRate; _dirty = true; }
    }

    /// <summary>Wendet den EQ in-place auf einen interleaved Float-Block an.</summary>
    public void Process(float[] buffer, int offset, int count, int channels)
    {
        lock (_gate)
        {
            if (!_enabled || _bands is null || _bands.Count == 0) return;

            if (_dirty || _filters.Length != channels)
            {
                _filters = new Biquad[channels][];
                for (var c = 0; c < channels; c++)
                    _filters[c] = _bands.Select(b => b.CreateFilter(_sampleRate)).ToArray();
                _dirty = false;
            }

            for (var i = 0; i < count; i += channels)
            {
                for (var c = 0; c < channels && i + c < count; c++)
                {
                    var s = buffer[offset + i + c];
                    foreach (var f in _filters[c]) s = f.Process(s);
                    buffer[offset + i + c] = s;
                }
            }
        }
    }
}
