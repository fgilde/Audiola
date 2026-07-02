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
    public MasteringPanel() => InitializeComponent();

    private void Waveform_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is WaveformControl wf && wf.ActualWidth > 0 && DataContext is MasteringViewModel vm)
            vm.Transport.Seek(e.GetPosition(wf).X / wf.ActualWidth);
    }
}
