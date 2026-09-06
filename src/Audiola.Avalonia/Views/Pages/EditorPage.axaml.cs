using Audiola.Avalonia.Platform;
using Audiola.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Audiola.Avalonia.Views.Pages;

/// <summary>
/// Wellenform-Editor. Die Maus- und Tastaturbedienung folgt dem, was in Audio-Programmen
/// üblich ist: links ziehen wählt aus, links klicken setzt die Abspielmarke, doppelt klicken
/// wählt alles, mit Umschalt wird die Auswahl erweitert, und die rechte Taste öffnet das
/// Kontextmenü, ohne die Auswahl anzutasten.
/// </summary>
public partial class EditorPage : UserControl, INavigationAware
{
    private readonly EditorViewModel _viewModel;
    private bool _dragging;
    private double _dragStartRatio;

    public EditorPage(EditorViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public void OnNavigatedTo()
    {
        _viewModel.EnsureLoaded();
        _viewModel.OnActivatedFx();
        // Damit die Tastenkürzel ohne vorherigen Klick greifen.
        Focus();
    }

    public void OnNavigatedFrom() => _viewModel.OnDeactivatedFx();

    // ---- Maus ----

    private void Waveform_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Controls.WaveformControl wf || wf.Bounds.Width <= 0) return;

        var point = e.GetCurrentPoint(wf);

        // Rechte Taste: nur Kontextmenü. Früher startete jede Taste eine Auswahl und fing den
        // Zeiger ein — das Menü kam deshalb nie und die Auswahl sprang beim Rechtsklick weg.
        if (point.Properties.IsRightButtonPressed) return;
        if (!point.Properties.IsLeftButtonPressed) return;

        Focus();
        var ratio = point.Position.X / wf.Bounds.Width;

        // Doppelklick wählt die ganze Spur.
        if (e.ClickCount == 2)
        {
            _viewModel.SetSelection(0, 1);
            e.Handled = true;
            return;
        }

        // Umschalt erweitert eine bestehende Auswahl bis zum Klick, statt neu anzufangen.
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && _viewModel.HasSelection)
        {
            var anchor = Math.Abs(ratio - _viewModel.SelectionStart) > Math.Abs(ratio - _viewModel.SelectionEnd)
                ? _viewModel.SelectionStart
                : _viewModel.SelectionEnd;
            _viewModel.SetSelection(anchor, ratio);
            e.Handled = true;
            return;
        }

        _dragging = true;
        _dragStartRatio = ratio;
        e.Pointer.Capture(wf);
    }

    private void Waveform_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging || sender is not Controls.WaveformControl wf || wf.Bounds.Width <= 0) return;
        _viewModel.SetSelection(_dragStartRatio, e.GetPosition(wf).X / wf.Bounds.Width);
    }

    private void Waveform_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Controls.WaveformControl wf) return;
        e.Pointer.Capture(null);
        if (!_dragging) return;
        _dragging = false;

        var ratio = wf.Bounds.Width > 0 ? e.GetPosition(wf).X / wf.Bounds.Width : 0;

        // Kaum bewegt → als Klick werten: Auswahl löschen und an die Stelle springen.
        if (Math.Abs(ratio - _dragStartRatio) * wf.Bounds.Width < 4)
        {
            _viewModel.ClearSelectionCommand.Execute(null);
            _viewModel.Transport.Seek(ratio);
        }
    }

    // ---- Tastatur ----

    /// <summary>
    /// Die Kürzel des Editors. Leertaste und Pos1 bleiben dem Fenster überlassen (Wiedergabe),
    /// hier liegen nur die Bearbeitungsbefehle.
    /// </summary>
    private void Page_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!_viewModel.HasTrack) return;

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        switch (e.Key)
        {
            case Key.Delete or Key.Back when _viewModel.DeleteCommand.CanExecute(null):
                _viewModel.DeleteCommand.Execute(null);
                break;

            case Key.A when ctrl:
                _viewModel.SetSelection(0, 1);
                break;

            case Key.Z when ctrl && _viewModel.UndoCommand.CanExecute(null):
                _viewModel.UndoCommand.Execute(null);
                break;

            case Key.Escape:
                _viewModel.ClearSelectionCommand.Execute(null);
                break;

            default:
                return;
        }

        e.Handled = true;
    }

    // ---- Kontextmenü ----
    // Bewusst über Click-Handler statt Bindungen: ein Kontextmenü ist ein eigenes Fenster
    // außerhalb des Baums, Bindungen auf den Seiten-DataContext greifen dort nicht zuverlässig.

    private void Trim_Click(object? sender, RoutedEventArgs e) => Run(_viewModel.TrimCommand);
    private void Delete_Click(object? sender, RoutedEventArgs e) => Run(_viewModel.DeleteCommand);
    private void Silence_Click(object? sender, RoutedEventArgs e) => Run(_viewModel.SilenceCommand);
    private void FadeIn_Click(object? sender, RoutedEventArgs e) => Run(_viewModel.FadeInCommand);
    private void FadeOut_Click(object? sender, RoutedEventArgs e) => Run(_viewModel.FadeOutCommand);
    private void Normalize_Click(object? sender, RoutedEventArgs e) => Run(_viewModel.NormalizeCommand);
    private void Reverse_Click(object? sender, RoutedEventArgs e) => Run(_viewModel.ReverseCommand);
    private void Undo_Click(object? sender, RoutedEventArgs e) => Run(_viewModel.UndoCommand);
    private void SelectAll_Click(object? sender, RoutedEventArgs e) => _viewModel.SetSelection(0, 1);
    private void ClearSelection_Click(object? sender, RoutedEventArgs e) => Run(_viewModel.ClearSelectionCommand);

    private static void Run(System.Windows.Input.ICommand command)
    {
        if (command.CanExecute(null)) command.Execute(null);
    }
}
