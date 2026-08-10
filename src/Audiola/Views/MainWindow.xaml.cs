using Audiola.Services;
using Audiola.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Controls;
using System.Windows;

namespace Audiola.Views;

/// <summary>
/// Die DAW-Shell. Alle Aktionen (Öffnen, Speichern, Autosave, Updates, Navigation) liegen im
/// geteilten <see cref="MainWindowViewModel"/>; hier bleiben nur die WPF-Ereignisse:
/// Drag&amp;Drop, globale Tasten, Fenster-Schließen und das Verdrahten der Visualisierer.
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly IShellNavigation _navigationService;

    public MainWindowViewModel ViewModel { get; }

    public TransportViewModel Transport { get; }

    /// <summary>Zuletzt geöffnete Projekte/Dateien fürs Datei-Menü (geteilt mit der Startseite).</summary>
    public HomeViewModel Home => ViewModel.Home;

    /// <summary>Von App.OnStartup gesetzt: beim ersten Laden zu öffnende Datei (Doppelklick/CLI).</summary>
    public string? PendingStartupFile { get; set; }

    public MainWindow(
        MainWindowViewModel viewModel,
        TransportViewModel transport,
        IShellNavigation shellNavigation,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService,
        TimelineViewModel timeline,
        StemMixerEngine mixer)
    {
        ViewModel = viewModel;
        Transport = transport;
        _navigationService = shellNavigation;
        DataContext = this;

        InitializeComponent();

        ((ShellNavigation)shellNavigation).SetFrame(MainFrame);
        shellNavigation.Navigated += (_, page) => SyncRail(page);
        snackbarService.SetSnackbarPresenter(RootSnackbarPresenter);
        contentDialogService.SetDialogHost(RootContentDialogPresenter);

        // Start auf der Startseite (Projekte & zuletzt geöffnet); Öffnen springt ins Studio.
        // Ein per Doppelklick/Kommandozeile übergebener Pfad wird danach geöffnet.
        Loaded += async (_, _) =>
        {
            _navigationService.Navigate(ShellPage.Home);
            if (PendingStartupFile is { } f) { PendingStartupFile = null; await ViewModel.OpenPathAsync(f); }
            await ViewModel.AutoUpdateAsync();
        };
        Closing += OnWindowClosing;

        // Globale Studio-Shortcuts: Leertaste = Play/Pause, Pos1 = an den Anfang.
        PreviewKeyDown += OnGlobalKeyDown;

        // Echtzeit-Spektrum + Master-VU in der Transportleiste (folgen dem Studio-Mix).
        timeline.SpectrumUpdated += (_, bands) => Spectrum.SetLevels(bands);
        mixer.LevelUpdated += (_, lr) => LevelMeter.SetLevels(lr.L, lr.R);

        // Tastenkürzel: Strg+S speichern, Strg+Umschalt+S speichern unter, Strg+O öffnen.
        InputBindings.Add(new System.Windows.Input.KeyBinding(ViewModel.SaveProjectCommand,
            System.Windows.Input.Key.S, System.Windows.Input.ModifierKeys.Control));
        InputBindings.Add(new System.Windows.Input.KeyBinding(ViewModel.SaveProjectAsCommand,
            System.Windows.Input.Key.S, System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift));
        InputBindings.Add(new System.Windows.Input.KeyBinding(ViewModel.OpenFileCommand,
            System.Windows.Input.Key.O, System.Windows.Input.ModifierKeys.Control));
    }

    private void Transport_WaveformMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Controls.WaveformControl wf && wf.ActualWidth > 0)
            Transport.Seek(e.GetPosition(wf).X / wf.ActualWidth);
    }

    /// <summary>Leertaste = Play/Pause, Pos1 = Anfang — außer der Fokus liegt in einem Eingabefeld.</summary>
    private void OnGlobalKeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
    {
        if (System.Windows.Input.Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase
            or System.Windows.Controls.PasswordBox
            or System.Windows.Controls.ComboBox { IsEditable: true })
            return;

        if (e.Key == System.Windows.Input.Key.Space && Transport.HasTrack)
        {
            if (Transport.PlayPauseCommand.CanExecute(null)) Transport.PlayPauseCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Home && Transport.HasTrack)
        {
            Transport.Seek(0);
            e.Handled = true;
        }
    }

    private bool _forceClose;

    private async void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_forceClose || !ViewModel.IsDirty) return;

        e.Cancel = true; // erst fragen, dann ggf. wirklich schließen
        if (await ViewModel.ConfirmDiscardAsync())
        {
            _forceClose = true;
            await Dispatcher.BeginInvoke(new Action(Close));
        }
    }

    /// <summary>Eintrag aus „Letzte Projekte"/„Letzte Dateien" im Datei-Menü öffnen.</summary>
    private void RecentItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem { DataContext: RecentItem item })
            ViewModel.OpenRecentCommand.Execute(item);
    }

    /// <summary>Navigation aus Rail (RadioButton) und Menü (MenuItem) — Ziel steckt im Tag.</summary>
    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string key } && Enum.TryParse<ShellPage>(key, out var page))
            _navigationService.Navigate(page);
    }

    /// <summary>Markiert das Rail-Werkzeug der aktiven Seite (bzw. keins bei Menü-Zielen).</summary>
    private void SyncRail(ShellPage page)
    {
        foreach (var rb in RailItems.Children.OfType<System.Windows.Controls.RadioButton>()
                     .Concat(RailFooter.Children.OfType<System.Windows.Controls.RadioButton>()))
            rb.IsChecked = rb.Tag as string == page.ToString();
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasDroppableFiles(e) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnFileDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        await ViewModel.LoadInputsAsync(files);
    }

    private static bool HasDroppableFiles(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return false;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        return files.Any(MainWindowViewModel.IsSupportedInput);
    }
}
