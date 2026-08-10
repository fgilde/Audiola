using System.IO;
using System.IO.Compression;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Audiola.ViewModels;

/// <summary>
/// Zustand UND Logik der Shell: Öffnen (Audio/ZIP/Projekt), Speichern, Autosave, Projekt schließen,
/// Update-Prüfung, Fenstertitel und Statusleiste. Host-neutral, damit WPF- und Avalonia-Fenster
/// nur noch Fenster-Ereignisse (Drop, Tasten, Schließen) an diese Befehle weiterreichen.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private static readonly string[] SupportedExtensions =
        [".wav", ".mp3", ".flac", ".aiff", ".aif", ".m4a", ".ogg"];

    private static readonly FileFilter AudioAndZip =
        new("Audio & ZIP", "wav", "mp3", "flac", "aiff", "m4a", "ogg", "zip");

    private readonly IShellNavigation _navigation;
    private readonly INotifier _snackbar;
    private readonly IFileDialogs _files;
    private readonly IAppDialogs _dialogs;
    private readonly ProjectWorkspace _workspace;
    private readonly TimelineViewModel _timeline;
    private readonly SongMetadata _songMeta;
    private readonly IMetadataService _metadata;
    private readonly UpdateService _updates;
    private readonly UiTimer _autosaveTimer;
    private bool _autoSaving;

    [ObservableProperty]
    private string _applicationTitle = "Audiola";

    /// <summary>Text der Statusleiste — zeigt laufende Hintergrund-Arbeit (Stems, Stimmtausch …).</summary>
    [ObservableProperty]
    private string _status = "Bereit";

    /// <summary>True, solange eine Hintergrund-Aufgabe läuft (Statuskugel wird gelb).</summary>
    [ObservableProperty]
    private bool _isWorking;

    /// <summary>Zuletzt geöffnete Projekte/Dateien fürs Datei-Menü (geteilt mit der Startseite).</summary>
    public HomeViewModel Home { get; }

    /// <summary>Globale Transportleiste (Play/Stop, Position, Pegel) — auch von der Shell gebunden.</summary>
    public TransportViewModel Transport { get; }

    public MainWindowViewModel(
        IShellNavigation navigation,
        INotifier snackbar,
        IFileDialogs files,
        IAppDialogs dialogs,
        ProjectWorkspace workspace,
        TimelineViewModel timeline,
        SongMetadata songMeta,
        IMetadataService metadata,
        HomeViewModel home,
        UpdateService updates,
        TransportViewModel transport)
    {
        _navigation = navigation;
        _snackbar = snackbar;
        _files = files;
        _dialogs = dialogs;
        _workspace = workspace;
        _timeline = timeline;
        _songMeta = songMeta;
        _metadata = metadata;
        Home = home;
        _updates = updates;
        Transport = transport;

        // Statusleiste + Fenstertitel folgen dem Studio-Zustand (Hintergrund-Arbeit, Projektname, Dirty).
        _timeline.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(TimelineViewModel.SeparationStatus)
                or nameof(TimelineViewModel.IsSeparating))
            {
                IsWorking = _timeline.IsSeparating;
                Status = string.IsNullOrWhiteSpace(_timeline.SeparationStatus)
                    ? "Bereit" : _timeline.SeparationStatus;
            }
            else if (args.PropertyName is nameof(TimelineViewModel.CurrentProjectPath)
                or nameof(TimelineViewModel.IsDirty))
            {
                UpdateTitle();
            }
        };

        // Autosave: alle 2 Minuten still zum aktuellen Projektpfad (nur bei ungespeicherten Änderungen).
        _autosaveTimer = new UiTimer { Interval = TimeSpan.FromMinutes(2) };
        _autosaveTimer.Tick += async (_, _) => await AutoSaveAsync();
        _autosaveTimer.Start();
    }

    // ---- Öffnen ----

    /// <summary>Öffnet einen Pfad: .audiola als Projekt, sonst als Audiospur(en). Auch für Drop/Start.</summary>
    public async Task OpenPathAsync(string path)
    {
        if (path.EndsWith(".audiola", StringComparison.OrdinalIgnoreCase))
            await OpenProjectPathAsync(path);
        else
            await LoadInputsAsync([path]);
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var paths = await _files.OpenFilesAsync("Öffnen (Audio oder ZIP)", true, AudioAndZip,
            FileFilter.Audio, new FileFilter("ZIP-Archive", "zip"), FileFilter.Any);
        if (paths.Count > 0) await LoadInputsAsync(paths);
    }

    [RelayCommand]
    private async Task AddToStudioAsync()
    {
        var paths = await _files.OpenFilesAsync("Audio ins Studio hinzufügen", true, AudioAndZip,
            FileFilter.Audio, FileFilter.Any);
        if (paths.Count > 0) await LoadInputsAsync(paths);
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        var path = await _files.OpenFileAsync("Projekt öffnen", FileFilter.Project, FileFilter.Any);
        if (path is not null) await OpenProjectPathAsync(path);
    }

    /// <summary>Ein .audiola-Projekt laden (nach Rückfrage bei ungespeicherten Änderungen).</summary>
    private async Task OpenProjectPathAsync(string path)
    {
        if (!await ConfirmDiscardAsync()) return;
        try
        {
            await _workspace.OpenAsync(path);
            _navigation.Navigate(ShellPage.Timeline);
            _snackbar.Success("Projekt geladen", Path.GetFileName(path), 3);
        }
        catch (Exception ex)
        {
            _snackbar.Error("Öffnen fehlgeschlagen", ex.Message, 5);
        }
    }

    /// <summary>
    /// Lädt Dateien/ZIPs ins Studio: öffnet als erste Spur, wenn noch nichts da ist, sonst wird
    /// jede als weitere Spur angehängt. ZIP-Archive werden entpackt und alle enthaltenen
    /// Audiodateien einzeln geladen.
    /// </summary>
    public async Task LoadInputsAsync(IEnumerable<string> paths)
    {
        var list = paths.ToList();

        // Ein .audiola-Projekt öffnet ein ganzes Projekt (ersetzt das aktuelle) — hat Vorrang.
        var project = list.FirstOrDefault(p =>
            p.EndsWith(".audiola", StringComparison.OrdinalIgnoreCase) && File.Exists(p));
        if (project is not null) { await OpenProjectPathAsync(project); return; }

        var files = ExpandToAudioFiles(list);
        if (files.Count == 0)
        {
            _snackbar.Warning("Nichts geladen", "Keine unterstützten Audiodateien gefunden.", 3);
            return;
        }

        _navigation.Navigate(ShellPage.Timeline);
        var loaded = 0;
        foreach (var f in files)
        {
            try { await _timeline.AddAudioFileAsync(f, -1, 0); loaded++; }
            catch { /* einzelne defekte Datei überspringen */ }
        }
        if (loaded == 0) return;

        AdoptMetadataIfEmpty(files[0]);
        _snackbar.Success("Geladen",
            loaded == 1 ? Path.GetFileName(files[0]) : $"{loaded} Spuren hinzugefügt.", 2);
    }

    /// <summary>True, wenn der Pfad als Audio, ZIP oder Projekt geladen werden kann (für Drag&amp;Drop).</summary>
    public static bool IsSupportedInput(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".zip" or ".audiola" || SupportedExtensions.Contains(ext);
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
                    ZipFile.ExtractToDirectory(p, dir);
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

    /// <summary>Übernimmt die Tags einer geöffneten Datei in die projektweiten Metadaten (nur leere Felder).</summary>
    private void AdoptMetadataIfEmpty(string path)
    {
        try
        {
            var read = _metadata.Read(path);
            if (!read.IsEmpty) _songMeta.Apply(read, onlyFillEmpty: true);
        }
        catch { /* Tags sind optional */ }
    }

    // ---- Speichern / Schließen ----

    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        if (!EnsureHasContent()) return;
        await TrySaveAsync(forceDialog: false); // zum aktuellen Pfad, sonst Dialog
    }

    [RelayCommand]
    private async Task SaveProjectAsAsync()
    {
        if (!EnsureHasContent()) return;
        await TrySaveAsync(forceDialog: true);
    }

    [RelayCommand]
    private async Task CloseProjectAsync()
    {
        if (!_workspace.HasContent) return;
        if (!await ConfirmDiscardAsync()) return;
        _timeline.CloseProject();
        _songMeta.Clear();
        _navigation.Navigate(ShellPage.Home);
        _snackbar.Info("Projekt geschlossen", "Das Studio ist jetzt leer.", 2);
    }

    private bool EnsureHasContent()
    {
        if (_workspace.HasContent) return true;
        _snackbar.Warning("Nichts zu speichern", "Im Studio sind keine Spuren geladen.", 4);
        return false;
    }

    /// <summary>Speichert das Projekt (zum aktuellen Pfad bzw. per Dialog). Liefert true bei Erfolg.</summary>
    private async Task<bool> TrySaveAsync(bool forceDialog = false)
    {
        var path = _workspace.CurrentPath;
        if (forceDialog || string.IsNullOrEmpty(path))
        {
            path = await _files.SaveFileAsync("Projekt speichern",
                Path.GetFileName(path ?? "projekt.audiola"), FileFilter.Project, FileFilter.Any);
            if (path is null) return false;
            if (!path.EndsWith(".audiola", StringComparison.OrdinalIgnoreCase)) path += ".audiola";
        }

        try
        {
            await _workspace.SaveAsync(path);
            _snackbar.Success("Projekt gespeichert", Path.GetFileName(path), 3);
            return true;
        }
        catch (Exception ex)
        {
            _snackbar.Error("Speichern fehlgeschlagen", ex.Message, 5);
            return false;
        }
    }

    /// <summary>Stilles Autosave — nur wenn ein Projektpfad bekannt ist und Änderungen anstehen.
    /// Kein Dialog, kein Snackbar-Spam; Rückmeldung dezent über die Statusleiste.</summary>
    private async Task AutoSaveAsync()
    {
        if (_autoSaving) return;
        if (!_workspace.HasContent || !_workspace.IsDirty || string.IsNullOrEmpty(_workspace.CurrentPath)) return;

        _autoSaving = true;
        try
        {
            await _workspace.SaveAsync(_workspace.CurrentPath!);
            Status = $"Automatisch gespeichert · {DateTime.Now:HH:mm}";
        }
        catch { /* still — beim nächsten Tick erneut versuchen */ }
        finally { _autoSaving = false; }
    }

    /// <summary>Fragt bei ungespeicherten Änderungen nach. false = Vorgang abbrechen.</summary>
    public async Task<bool> ConfirmDiscardAsync()
    {
        if (!_workspace.IsDirty) return true;

        return await _dialogs.AskSaveDiscardCancelAsync(
            "Projekt speichern?", "Es gibt ungespeicherte Änderungen. Vorher speichern?") switch
        {
            SaveDiscardCancel.Save => await TrySaveAsync(),
            SaveDiscardCancel.Discard => true,
            _ => false
        };
    }

    /// <summary>True, wenn ungespeicherte Änderungen existieren (Fenster-Schließen abfangen).</summary>
    public bool IsDirty => _workspace.IsDirty;

    // ---- Updates ----

    /// <summary>
    /// Beim Start nach Updates suchen und eines anbieten (nur installierte Version). Bewusst mit
    /// Rückfrage statt still im Hintergrund: ein Neustart mitten in der Arbeit wäre überraschend,
    /// und ohne Hinweis merkt niemand, dass eine neue Fassung bereitliegt.
    /// </summary>
    public async Task AutoUpdateAsync()
    {
        try
        {
            var info = await _updates.CheckAsync();
            if (info is null) return;

            var version = info.TargetFullRelease.Version.ToString();
            Status = $"Update {version} verfügbar";
            _snackbar.Info("Update verfügbar",
                $"Version {version} steht bereit — über Hilfe → „Nach Updates suchen…“ installieren.", 8);

            if (!_dialogs.Confirm("Update verfügbar",
                    $"Version {version} ist verfügbar (installiert: {_updates.CurrentVersion})."
                    + Environment.NewLine + Environment.NewLine
                    + "Jetzt herunterladen und neu starten?"))
                return;

            IsWorking = true;
            Status = $"Update {version} wird geladen …";
            if (await _updates.DownloadAsync(info))
                _updates.ApplyAndRestart(info);
            else
                _snackbar.Error("Update fehlgeschlagen", "Siehe audiola.log.", 6);
        }
        catch { /* Update-Fehler dürfen den Start nicht stören */ }
        finally { IsWorking = false; }
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        if (!_updates.IsManaged)
        {
            _snackbar.Info("Updates",
                "Automatische Updates gibt es nur in der installierten Version (Setup von GitHub).", 5);
            return;
        }

        _snackbar.Info("Updates", "Suche nach Updates …", 2);

        var info = await _updates.CheckAsync();
        if (info is null)
        {
            _snackbar.Success("Updates", "Du hast die neueste Version.", 3);
            return;
        }

        var newVer = info.TargetFullRelease.Version.ToString();
        if (!_dialogs.Confirm("Audiola-Update",
                $"Eine neue Version ist verfügbar: v{newVer}.\n\nJetzt herunterladen und neu starten?"))
            return;

        if (await _updates.DownloadAsync(info))
            _updates.ApplyAndRestart(info);
        else
            _snackbar.Error("Update fehlgeschlagen", "Siehe audiola.log.", 5);
    }

    // ---- Navigation aus Menü/Rail ----

    [RelayCommand]
    private void Navigate(ShellPage page) => _navigation.Navigate(page);

    [RelayCommand]
    private void ShowSetupWizard() => _dialogs.ShowSetupWizard();

    /// <summary>Eintrag aus „Letzte Projekte"/„Letzte Dateien" öffnen.</summary>
    [RelayCommand]
    private async Task OpenRecentAsync(RecentItem? item)
    {
        if (item is not null) await Home.OpenCommand.ExecuteAsync(item);
    }

    /// <summary>Fenstertitel: „Audiola — Projektname •" (Punkt = ungespeicherte Änderungen).</summary>
    private void UpdateTitle()
    {
        var name = string.IsNullOrEmpty(_timeline.CurrentProjectPath)
            ? null : Path.GetFileNameWithoutExtension(_timeline.CurrentProjectPath);
        ApplicationTitle = name is null
            ? "Audiola"
            : $"Audiola — {name}{(_timeline.IsDirty ? " •" : "")}";
    }
}
