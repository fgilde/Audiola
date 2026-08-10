using Audiola.Avalonia.Platform;
using Audiola.Services;
using Audiola.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Audiola.Avalonia.Views;

/// <summary>
/// Die DAW-Shell. Alle Aktionen (Öffnen, Speichern, Autosave, Updates, Navigation) liegen im
/// geteilten <see cref="MainWindowViewModel"/>; hier bleiben nur die Avalonia-Ereignisse:
/// Drag&amp;Drop, globale Tasten, Fenster-Schließen und das Verdrahten der Visualisierer.
/// </summary>
public partial class MainWindow : Window
{
    private readonly IShellNavigation _navigation;
    private readonly MainWindowViewModel _viewModel;
    private bool _forceClose;

    /// <summary>Von der App gesetzt: beim ersten Anzeigen zu öffnende Datei (Doppelklick/CLI).</summary>
    public string? PendingStartupFile { get; set; }

    public MainWindow(
        MainWindowViewModel viewModel,
        IShellNavigation navigation,
        AvaloniaNotifier notifier,
        TimelineViewModel timeline,
        StemMixerEngine mixer)
    {
        _viewModel = viewModel;
        _navigation = navigation;
        DataContext = viewModel;

        InitializeComponent();

        ((AvaloniaShellNavigation)navigation).SetHost(PageHost);
        navigation.Navigated += (_, page) => SyncRail(page);
        notifier.SetHost(RootSnackbarHost);

        // Echtzeit-Spektrum + Master-VU in der Transportleiste (folgen dem Studio-Mix).
        timeline.SpectrumUpdated += (_, bands) => Spectrum.SetLevels(bands);
        mixer.LevelUpdated += (_, lr) => LevelMeter.SetLevels(lr.L, lr.R);

        // Statuskugel folgt der Hintergrund-Arbeit (Style-Klasse statt DataTrigger).
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsWorking))
                StatusDot.Classes.Set("working", viewModel.IsWorking);
        };

        // Start auf der Startseite (Projekte & zuletzt geöffnet); Öffnen springt ins Studio.
        Opened += async (_, _) =>
        {
            _navigation.Navigate(ShellPage.Home);
            if (PendingStartupFile is { } f) { PendingStartupFile = null; await _viewModel.OpenPathAsync(f); }
            await _viewModel.AutoUpdateAsync();
        };
        Closing += OnWindowClosing;

        // Globale Studio-Shortcuts: Leertaste = Play/Pause, Pos1 = an den Anfang.
        AddHandler(KeyDownEvent, OnGlobalKeyDown, RoutingStrategies.Tunnel);

        // Dateien ins Fenster ziehen (Audio, ZIP, Projekt).
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnFileDrop);
    }

    private void Transport_WaveformPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Controls.WaveformControl wf && wf.Bounds.Width > 0)
            _viewModel.Transport.Seek(e.GetPosition(wf).X / wf.Bounds.Width);
    }

    /// <summary>Leertaste = Play/Pause, Pos1 = Anfang — außer der Fokus liegt in einem Eingabefeld.</summary>
    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        if (FocusManager?.GetFocusedElement() is TextBox or AutoCompleteBox
            || (FocusManager?.GetFocusedElement() is ComboBox { IsEditable: true }))
            return;

        var transport = _viewModel.Transport;
        if (e.Key == Key.Space && transport.HasTrack)
        {
            if (transport.PlayPauseCommand.CanExecute(null)) transport.PlayPauseCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Home && transport.HasTrack)
        {
            transport.Seek(0);
            e.Handled = true;
        }
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_forceClose || !_viewModel.IsDirty) return;

        e.Cancel = true; // erst fragen, dann ggf. wirklich schließen
        if (await _viewModel.ConfirmDiscardAsync())
        {
            _forceClose = true;
            Dispatcher.UIThread.Post(Close);
        }
    }

    /// <summary>Navigation aus der Rail — Ziel steckt im Tag.</summary>
    private void Nav_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: string key } && Enum.TryParse<ShellPage>(key, out var page))
            _navigation.Navigate(page);
    }

    /// <summary>Markiert das Rail-Werkzeug der aktiven Seite (bzw. keins bei Menü-Zielen).</summary>
    private void SyncRail(ShellPage page)
    {
        foreach (var rb in RailItems.Children.OfType<RadioButton>()
                     .Concat(RailFooter.Children.OfType<RadioButton>()))
            rb.IsChecked = rb.Tag as string == page.ToString();
    }

    private void Exit_Click(object? sender, RoutedEventArgs e) => Close();

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles()?.Select(f => f.Path.LocalPath) ?? [];
        e.DragEffects = files.Any(MainWindowViewModel.IsSupportedInput)
            ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnFileDrop(object? sender, DragEventArgs e)
    {
        var files = e.Data.GetFiles()?.Select(f => f.Path.LocalPath).ToList();
        if (files is { Count: > 0 }) await _viewModel.LoadInputsAsync(files);
    }
}
