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

    private readonly IShellNavigation _navigationService;
    private readonly ISnackbarService _snackbarService;
    private readonly UpdateService _updates = new();

    public MainWindowViewModel ViewModel { get; }

    public TransportViewModel Transport { get; }

    public MainWindow(
        MainWindowViewModel viewModel,
        TransportViewModel transport,
        IShellNavigation shellNavigation,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService)
    {
        ViewModel = viewModel;
        Transport = transport;
        _navigationService = shellNavigation;
        _snackbarService = snackbarService;
        DataContext = this;

        InitializeComponent();

        ((ShellNavigation)shellNavigation).SetFrame(MainFrame);
        shellNavigation.Navigated += (_, t) => SyncRail(t);
        snackbarService.SetSnackbarPresenter(RootSnackbarPresenter);
        contentDialogService.SetDialogHost(RootContentDialogPresenter);

        // Start direkt im Studio — die Arbeitsfläche ist das Zentrum der App.
        Loaded += (_, _) => _navigationService.Navigate(typeof(Views.Pages.TimelinePage));
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
    private const string AudioFilter =
        "Audio & ZIP|*.wav;*.mp3;*.flac;*.aiff;*.m4a;*.ogg;*.zip|" +
        "Audiodateien|*.wav;*.mp3;*.flac;*.aiff;*.m4a;*.ogg|" +
        "ZIP-Archive|*.zip|Alle Dateien|*.*";

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Öffnen (Audio oder ZIP)", Multiselect = true, Filter = AudioFilter };
        if (dlg.ShowDialog() != true) return;
        await LoadInputsAsync(dlg.FileNames);
    }

    private async void AddToStudio_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Audio ins Studio hinzufügen", Multiselect = true, Filter = AudioFilter };
        if (dlg.ShowDialog() != true) return;
        await LoadInputsAsync(dlg.FileNames);
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

    /// <summary>Navigation aus Rail (RadioButton) und Menü (MenuItem) — Ziel steckt im Tag.</summary>
    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Type t })
            _navigationService.Navigate(t);
    }

    /// <summary>Markiert das Rail-Werkzeug der aktiven Seite (bzw. keins bei Menü-Zielen).</summary>
    private void SyncRail(Type pageType)
    {
        foreach (var rb in RailItems.Children.OfType<System.Windows.Controls.RadioButton>()
                     .Concat(RailFooter.Children.OfType<System.Windows.Controls.RadioButton>()))
            rb.IsChecked = rb.Tag as Type == pageType;
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
        e.Effects = HasDroppableFiles(e) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnFileDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        await LoadInputsAsync(files);
    }

    private static bool HasDroppableFiles(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return false;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        return files.Any(IsSupportedInput);
    }

    private static bool IsSupportedInput(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext == ".zip" || SupportedExtensions.Contains(ext);
    }

    /// <summary>
    /// Lädt Dateien/ZIPs ins Studio: öffnet als erste Spur, wenn noch nichts da ist, sonst wird
    /// jede als weitere Spur angehängt. ZIP-Archive werden entpackt und alle enthaltenen
    /// Audiodateien einzeln geladen.
    /// </summary>
    private async Task LoadInputsAsync(IEnumerable<string> paths)
    {
        var files = ExpandToAudioFiles(paths);
        if (files.Count == 0)
        {
            _snackbarService.Show("Nichts geladen", "Keine unterstützten Audiodateien gefunden.",
                ControlAppearance.Caution, new SymbolIcon(SymbolRegular.Warning24), TimeSpan.FromSeconds(3));
            return;
        }

        _navigationService.Navigate(typeof(Views.Pages.TimelinePage));
        var vm = App.GetService<ViewModels.TimelineViewModel>();
        int loaded = 0;
        foreach (var f in files)
        {
            try { await vm.AddAudioFileAsync(f, -1, 0); loaded++; }
            catch { /* einzelne defekte Datei überspringen */ }
        }
        if (loaded == 0) return;

        AdoptMetadataIfEmpty(files[0]);
        _snackbarService.Show("Geladen",
            loaded == 1 ? Path.GetFileName(files[0]) : $"{loaded} Spuren hinzugefügt.",
            ControlAppearance.Success, new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(2));
    }

    /// <summary>Expandiert Eingabepfade: ZIPs werden nach %Temp% entpackt; übrig bleiben nur unterstützte Audiodateien.</summary>
    private static List<string> ExpandToAudioFiles(IEnumerable<string> paths)
    {
        var result = new List<string>();
        foreach (var p in paths)
        {
            if (Path.GetExtension(p).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var dir = Path.Combine(Path.GetTempPath(), "Audiola", "zip", Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(dir);
                    System.IO.Compression.ZipFile.ExtractToDirectory(p, dir);
                    result.AddRange(Directory
                        .EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                        .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase));
                }
                catch { /* defektes/gesperrtes Archiv überspringen */ }
            }
            else if (SupportedExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()))
            {
                result.Add(p);
            }
        }
        return result;
    }
}
