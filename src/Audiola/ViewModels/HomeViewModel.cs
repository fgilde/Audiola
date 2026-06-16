using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Audiola.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly ITrackLoader _loader;
    private readonly INavigationService _navigation;
    private readonly ISnackbarService _snackbar;
    private readonly TimelineViewModel _timeline;
    private readonly ProjectWorkspace _workspace;

    public SessionState Session { get; }

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<RecentItem> RecentFiles { get; } = [];
    public ObservableCollection<RecentItem> RecentProjects { get; } = [];

    public bool HasRecent => RecentFiles.Count > 0;
    public bool HasRecentProjects => RecentProjects.Count > 0;

    public HomeViewModel(
        SessionState session,
        ITrackLoader loader,
        INavigationService navigation,
        ISnackbarService snackbar,
        TimelineViewModel timeline,
        ProjectWorkspace workspace)
    {
        Session = session;
        _loader = loader;
        _navigation = navigation;
        _snackbar = snackbar;
        _timeline = timeline;
        _workspace = workspace;

        _loader.RecentChanged += (_, _) => RefreshRecent();
        _workspace.RecentChanged += (_, _) => RefreshRecentProjects();
        RefreshRecent();
        RefreshRecentProjects();
    }

    private void RefreshRecent()
    {
        RecentFiles.Clear();
        foreach (var path in _loader.RecentFiles)
            RecentFiles.Add(new RecentItem(path, Path.GetFileName(path), isProject: false));
        OnPropertyChanged(nameof(HasRecent));
    }

    /// <summary>Aktualisiert die Projektliste (auch beim erneuten Anzeigen der Startseite aufrufen).</summary>
    public void RefreshRecentProjects()
    {
        RecentProjects.Clear();
        foreach (var path in _workspace.RecentProjects)
            RecentProjects.Add(new RecentItem(path, Path.GetFileNameWithoutExtension(path), isProject: true));
        OnPropertyChanged(nameof(HasRecentProjects));
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Audiodatei öffnen",
            Filter = "Audiodateien|*.wav;*.mp3;*.flac;*.aiff;*.m4a;*.ogg|Alle Dateien|*.*"
        };
        if (dialog.ShowDialog() == true)
            await Load(dialog.FileName);
    }

    // ---- Einheitliche Aktionen für beide Listen ----

    [RelayCommand]
    private async Task OpenAsync(RecentItem? item)
    {
        if (item is null) return;
        if (item.IsProject)
        {
            IsLoading = true;
            try
            {
                await _workspace.OpenAsync(item.Path);
                _navigation.Navigate(typeof(Views.Pages.TimelinePage));
                _snackbar.Show("Projekt geladen", item.Name, ControlAppearance.Success,
                    new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                _snackbar.Show("Öffnen fehlgeschlagen", ex.Message, ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(4));
            }
            finally { IsLoading = false; }
        }
        else
        {
            await Load(item.Path);
        }
    }

    [RelayCommand]
    private void Remove(RecentItem? item)
    {
        if (item is null) return;
        if (item.IsProject) _workspace.RemoveRecent(item.Path);
        else _loader.RemoveRecent(item.Path);
    }

    [RelayCommand]
    private void Delete(RecentItem? item)
    {
        if (item is null) return;
        var what = item.IsProject ? "Projektdatei" : "Audiodatei";
        if (!Confirm($"{what} wirklich von der Festplatte löschen?\n\n{item.Path}")) return;
        TryDelete(item.Path, item.IsProject ? "Projekt gelöscht" : "Datei gelöscht");
        if (item.IsProject) _workspace.RemoveRecent(item.Path);
        else _loader.RemoveRecent(item.Path);
    }

    private static bool Confirm(string message) =>
        System.Windows.MessageBox.Show(message, "Löschen bestätigen",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning)
            == System.Windows.MessageBoxResult.Yes;

    private void TryDelete(string path, string okText)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
            _snackbar.Show(okText, Path.GetFileName(path), ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            _snackbar.Show("Löschen fehlgeschlagen", ex.Message, ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(4));
        }
    }

    private async Task Load(string path)
    {
        IsLoading = true;
        try
        {
            var track = await _loader.LoadAsync(path);
            await _timeline.AddAudioFileAsync(path, -1, 0); // als erste/weitere Studio-Spur
            _snackbar.Show("Geladen", track.FileName, ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(2));
            _navigation.Navigate(typeof(Views.Pages.TimelinePage));
        }
        catch (Exception ex)
        {
            _snackbar.Show("Fehler beim Laden", ex.Message, ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(4));
        }
        finally
        {
            IsLoading = false;
        }
    }
}

/// <summary>Eintrag in „Letzte Projekte"/„Zuletzt geöffnet" inkl. Detailangaben.</summary>
public sealed class RecentItem
{
    public string Path { get; }
    public string Name { get; }
    public string Folder { get; }
    public bool Exists { get; }
    public bool IsProject { get; }
    public string DetailsText { get; }

    public Wpf.Ui.Controls.SymbolRegular IconSymbol =>
        IsProject ? Wpf.Ui.Controls.SymbolRegular.FolderOpen24 : Wpf.Ui.Controls.SymbolRegular.MusicNote224;

    public RecentItem(string path, string name, bool isProject)
    {
        Path = path;
        Name = name;
        IsProject = isProject;
        Folder = System.IO.Path.GetDirectoryName(path) ?? "";
        try
        {
            var fi = new FileInfo(path);
            Exists = fi.Exists;
            DetailsText = Exists
                ? $"{FormatSize(fi.Length)}  ·  {fi.LastWriteTime:dd.MM.yyyy HH:mm}"
                : "Datei fehlt";
        }
        catch { Exists = false; DetailsText = "Datei fehlt"; }
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var i = 0;
        while (size >= 1024 && i < units.Length - 1) { size /= 1024; i++; }
        return $"{size:0.#} {units[i]}";
    }
}
