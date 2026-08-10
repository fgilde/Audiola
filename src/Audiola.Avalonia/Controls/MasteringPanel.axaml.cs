using Audiola.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Audiola.Controls;

/// <summary>
/// Gemeinsames Mastering-Panel (Vorschau mit Wellenform/Seek/A/B, Presets, EQ, Kompressor, LUFS) —
/// identisch auf der Mastering-Seite (Studio-Mix) und im „Spur mastern"-Dialog (Einzelspur).
/// Erwartet ein <see cref="MasteringViewModel"/> als DataContext.
/// </summary>
public partial class MasteringPanel : UserControl
{
    /// <summary>Aktionen-Zeile (Anwenden/Exportieren) anzeigen? Der Dialog blendet sie aus
    /// und stellt die Buttons stattdessen in seinen Footer (Anwenden/Abbrechen-Stil).</summary>
    public static readonly StyledProperty<bool> ShowActionsProperty =
        AvaloniaProperty.Register<MasteringPanel, bool>(nameof(ShowActions), true);

    public bool ShowActions
    {
        get => GetValue(ShowActionsProperty);
        set => SetValue(ShowActionsProperty, value);
    }

    public MasteringPanel() => InitializeComponent();

    private void Waveform_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is WaveformControl wf && wf.Bounds.Width > 0 && DataContext is MasteringViewModel vm)
            vm.Transport.Seek(e.GetPosition(wf).X / wf.Bounds.Width);
    }
}
