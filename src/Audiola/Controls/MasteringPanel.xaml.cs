using System.Windows.Controls;
using Audiola.ViewModels;

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
    public static readonly System.Windows.DependencyProperty ShowActionsProperty =
        System.Windows.DependencyProperty.Register(nameof(ShowActions), typeof(bool),
            typeof(MasteringPanel), new System.Windows.PropertyMetadata(true));

    public bool ShowActions
    {
        get => (bool)GetValue(ShowActionsProperty);
        set => SetValue(ShowActionsProperty, value);
    }

    public MasteringPanel() => InitializeComponent();

    private void Waveform_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is WaveformControl wf && wf.ActualWidth > 0 && DataContext is MasteringViewModel vm)
            vm.Transport.Seek(e.GetPosition(wf).X / wf.ActualWidth);
    }
}
