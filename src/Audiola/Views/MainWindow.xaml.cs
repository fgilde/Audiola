using System.IO;
using System.Windows;
using Audiola.Services;
using Audiola.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Audiola.Views;

public partial class MainWindow : FluentWindow
{
    private static readonly string[] SupportedExtensions =
        [".wav", ".mp3", ".flac", ".aiff", ".aif", ".m4a", ".ogg"];

    private readonly INavigationService _navigationService;
    private readonly ITrackLoader _trackLoader;
    private readonly ISnackbarService _snackbarService;
    private readonly UpdateService _updates = new();

    public MainWindowViewModel ViewModel { get; }

    public TransportViewModel Transport { get; }

    public MainWindow(
        MainWindowViewModel viewModel,
        TransportViewModel transport,
        IServiceProvider serviceProvider,
        INavigationService navigationService,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService,
        ITrackLoader trackLoader)
    {
        ViewModel = viewModel;
        Transport = transport;
        _navigationService = navigationService;
        _snackbarService = snackbarService;
        _trackLoader = trackLoader;
        DataContext = this;

        InitializeComponent();

        RootNavigation.SetServiceProvider(serviceProvider);
        navigationService.SetNavigationControl(RootNavigation);
        snackbarService.SetSnackbarPresenter(RootSnackbarPresenter);
        contentDialogService.SetDialogHost(RootContentDialogPresenter);

        // Erst navigieren, wenn das NavigationView-Template angewandt ist.
        Loaded += (_, _) => RootNavigation.Navigate(typeof(Views.Pages.HomePage));
        Closing += OnWindowClosing;

        // Leises Auto-Update beim Start (nur in installierter Version).
        Loaded += async (_, _) => await AutoUpdateAsync();

        // Echtzeit-Spektrum in der Menüleiste (folgt dem Studio-Mix).
        App.GetService<ViewModels.TimelineViewModel>().SpectrumUpdated += (_, bands) => Spectrum.SetLevels(bands);

        // Strg+S → Projekt speichern.
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            new CommunityToolkit.Mvvm.Input.RelayCommand(() => SaveProjectQuick_Click(this, new RoutedEventArgs())),
            System.Windows.Input.Key.S, System.Windows.Input.ModifierKeys.Control));
    }

    private void Transport_WaveformMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Controls.WaveformControl wf && wf.ActualWidth > 0)
            Transport.Seek(e.GetPosition(wf).X / wf.ActualWidth);
    }

    // ---- Menü ----
    private const string AudioFilter = "Audiodateien|*.wav;*.mp3;*.flac;*.aiff;*.m4a;*.ogg|Alle Dateien|*.*";

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Audiodatei öffnen", Filter = AudioFilter };
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _trackLoader.LoadAsync(dlg.FileName);
            await App.GetService<ViewModels.TimelineViewModel>().AddAudioFileAsync(dlg.FileName, -1, 0);
            AdoptMetadataIfEmpty(dlg.FileName);
            _navigationService.Navigate(typeof(Views.Pages.TimelinePage));
        }
        catch (Exception ex)
        {
            _snackbarService.Show("Fehler beim Laden", ex.Message, ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(4));
        }
    }

    private async void AddToStudio_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Audio ins Studio hinzufügen", Multiselect = true, Filter = AudioFilter };
        if (dlg.ShowDialog() != true) return;
        _navigationService.Navigate(typeof(Views.Pages.TimelinePage));
        var vm = App.GetService<ViewModels.TimelineViewModel>();
        foreach (var f in dlg.FileNames)
            await vm.AddAudioFileAsync(f, -1, 0);
        if (dlg.FileNames.Length > 0) AdoptMetadataIfEmpty(dlg.FileNames[0]);
    }

    /// <summary>Übernimmt die Tags einer geöffneten Datei in die projektweiten Metadaten (nur leere Felder).</summary>
    private static void AdoptMetadataIfEmpty(string path)
    {
        try
        {
            var read = App.GetService<IMetadataService>().Read(path);
            if (!read.IsEmpty)
                App.GetService<ViewModels.SongMetadata>().Apply(read, onlyFillEmpty: true);
        }
        catch { /* Tags sind optional */ }
    }

    private const string ProjectFilter = "Audiola-Projekt|*.audiola|Alle Dateien|*.*";

    private async void SaveProjectQuick_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureHasContent()) return;
        await TrySaveAsync(forceDialog: false); // zum aktuellen Pfad, sonst Dialog
    }

    private async void SaveProjectAs_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureHasContent()) return;
        await TrySaveAsync(forceDialog: true);
    }

    private async void CloseProject_Click(object sender, RoutedEventArgs e)
    {
        if (!App.GetService<ProjectWorkspace>().HasContent) return;
        if (!await ConfirmDiscardAsync()) return;
        App.GetService<ViewModels.TimelineViewModel>().CloseProject();
        App.GetService<ViewModels.SongMetadata>().Clear();
        _navigationService.Navigate(typeof(Views.Pages.HomePage));
        _snackbarService.Show("Projekt geschlossen", "Das Studio ist jetzt leer.",
            ControlAppearance.Secondary, new SymbolIcon(SymbolRegular.Info24), TimeSpan.FromSeconds(2));
    }

    private bool EnsureHasContent()
    {
        if (App.GetService<ProjectWorkspace>().HasContent) return true;
        _snackbarService.Show("Nichts zu speichern", "Im Studio sind keine Spuren geladen.",
            ControlAppearance.Caution, new SymbolIcon(SymbolRegular.Warning24), TimeSpan.FromSeconds(4));
        return false;
    }

    private async void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        // Vor dem Öffnen ggf. das aktuelle Projekt sichern.
        if (!await ConfirmDiscardAsync()) return;

        var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Projekt öffnen", Filter = ProjectFilter };
        if (dlg.ShowDialog() != true) return;

        try
        {
            await App.GetService<ProjectWorkspace>().OpenAsync(dlg.FileName);
            _navigationService.Navigate(typeof(Views.Pages.TimelinePage));
            _snackbarService.Show("Projekt geladen", Path.GetFileName(dlg.FileName),
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbarService.Show("Öffnen fehlgeschlagen", ex.Message,
                ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(5));
        }
    }

    /// <summary>Speichert das Projekt (zum aktuellen Pfad bzw. per Dialog). Liefert true bei Erfolg.</summary>
    private async Task<bool> TrySaveAsync(bool forceDialog = false)
    {
        var ws = App.GetService<ProjectWorkspace>();
        var path = ws.CurrentPath;
        if (forceDialog || string.IsNullOrEmpty(path))
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Projekt speichern",
                Filter = ProjectFilter,
                FileName = Path.GetFileName(path ?? "projekt.audiola"),
                DefaultExt = ".audiola"
            };
            if (dlg.ShowDialog() != true) return false;
            path = dlg.FileName;
        }

        try
        {
            await ws.SaveAsync(path!);
            _snackbarService.Show("Projekt gespeichert", Path.GetFileName(path!),
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(3));
            return true;
        }
        catch (Exception ex)
        {
            _snackbarService.Show("Speichern fehlgeschlagen", ex.Message,
                ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(5));
            return false;
        }
    }

    /// <summary>Fragt bei ungespeicherten Änderungen nach. false = Vorgang abbrechen.</summary>
    private async Task<bool> ConfirmDiscardAsync()
    {
        var ws = App.GetService<ProjectWorkspace>();
        if (!ws.IsDirty) return true;

        var box = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Projekt speichern?",
            Content = "Es gibt ungespeicherte Änderungen. Vorher speichern?",
            PrimaryButtonText = "Speichern",
            SecondaryButtonText = "Verwerfen",
            CloseButtonText = "Abbrechen"
        };
        var result = await box.ShowDialogAsync();
        return result switch
        {
            Wpf.Ui.Controls.MessageBoxResult.Primary => await TrySaveAsync(),
            Wpf.Ui.Controls.MessageBoxResult.Secondary => true, // verwerfen
            _ => false                                          // abbrechen
        };
    }

    private bool _forceClose;

    private async void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_forceClose) return;
        var ws = App.GetService<ProjectWorkspace>();
        if (!ws.IsDirty) return;

        e.Cancel = true; // erst fragen, dann ggf. wirklich schließen
        if (await ConfirmDiscardAsync())
        {
            _forceClose = true;
            // Close() NICHT direkt aufrufen — wir sind noch im Closing-Handler, das wirft
            // „Close cannot be called while a Window is in a Closing event handler". Stattdessen
            // über den Dispatcher nachreichen, damit es nach Abschluss dieses Events läuft.
            await Dispatcher.BeginInvoke(new Action(Close));
        }
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem { Tag: Type t })
            _navigationService.Navigate(t);
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (Transport.PlayPauseCommand.CanExecute(null)) Transport.PlayPauseCommand.Execute(null);
    }

    private void StopPlay_Click(object sender, RoutedEventArgs e)
    {
        if (Transport.StopCommand.CanExecute(null)) Transport.StopCommand.Execute(null);
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();

    /// <summary>Beim Start still nach Updates suchen und ggf. anwenden (nur installierte Version).</summary>
    private async Task AutoUpdateAsync()
    {
        try
        {
            var info = await _updates.CheckAsync();
            if (info is null) return;
            if (await _updates.DownloadAsync(info))
                _updates.ApplyAndRestart(info);
        }
        catch { /* Update-Fehler dürfen den Start nicht stören */ }
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        if (!_updates.IsManaged)
        {
            _snackbarService.Show("Updates",
                "Automatische Updates gibt es nur in der installierten Version (Setup von GitHub).",
                ControlAppearance.Secondary, new SymbolIcon(SymbolRegular.Info24), TimeSpan.FromSeconds(5));
            return;
        }

        _snackbarService.Show("Updates", "Suche nach Updates …",
            ControlAppearance.Secondary, new SymbolIcon(SymbolRegular.ArrowSync24), TimeSpan.FromSeconds(2));

        var info = await _updates.CheckAsync();
        if (info is null)
        {
            _snackbarService.Show("Updates", "Du hast die neueste Version.",
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(3));
            return;
        }

        var newVer = info.TargetFullRelease.Version.ToString();
        var ask = System.Windows.MessageBox.Show(
            $"Eine neue Version ist verfügbar: v{newVer}.\n\nJetzt herunterladen und neu starten?",
            "Audiola-Update", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information);
        if (ask != System.Windows.MessageBoxResult.Yes) return;

        if (await _updates.DownloadAsync(info))
            _updates.ApplyAndRestart(info);
        else
            _snackbarService.Show("Update fehlgeschlagen", "Siehe audiola.log.",
                ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(5));
    }

    private void About_Click(object sender, RoutedEventArgs e)
        => _navigationService.Navigate(typeof(Views.Pages.AboutPage));

    private void SetupWizard_Click(object sender, RoutedEventArgs e)
    {
        var wizard = App.GetService<Views.Dialogs.SetupWizardWindow>();
        wizard.Owner = this;
        wizard.ShowDialog();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = TryGetAudioFile(e, out _) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnFileDrop(object sender, DragEventArgs e)
    {
        if (!TryGetAudioFile(e, out var path))
            return;

        try
        {
            var track = await _trackLoader.LoadAsync(path!);
            await App.GetService<ViewModels.TimelineViewModel>().AddAudioFileAsync(path!, -1, 0);
            AdoptMetadataIfEmpty(path!);
            _snackbarService.Show("Geladen", track.FileName, ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(2));
            _navigationService.Navigate(typeof(Views.Pages.TimelinePage));
        }
        catch (Exception ex)
        {
            _snackbarService.Show("Fehler beim Laden", ex.Message, ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(4));
        }
    }

    private static bool TryGetAudioFile(DragEventArgs e, out string? path)
    {
        path = null;
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return false;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        path = files.FirstOrDefault(f =>
            SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
        return path is not null;
    }
}
