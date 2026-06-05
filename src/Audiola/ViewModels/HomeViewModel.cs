using System.Collections.ObjectModel;
using System.IO;
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

    /// <summary>Anzeigeobjekte für „Zuletzt geöffnet“ (voller Pfad + Dateiname).</summary>
    public ObservableCollection<RecentFile> RecentFiles { get; } = [];

    /// <summary>Zuletzt geöffnete Projekte (.audiola).</summary>
    public ObservableCollection<RecentFile> RecentProjects { get; } = [];

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
            RecentFiles.Add(new RecentFile(path, Path.GetFileName(path)));
        OnPropertyChanged(nameof(HasRecent));
    }

    /// <summary>Aktualisiert die Projektliste (auch beim erneuten Anzeigen der Startseite aufrufen).</summary>
    public void RefreshRecentProjects()
    {
        RecentProjects.Clear();
        foreach (var path in _workspace.RecentProjects)
            RecentProjects.Add(new RecentFile(path, Path.GetFileNameWithoutExtension(path)));
        OnPropertyChanged(nameof(HasRecentProjects));
    }

    [RelayCommand]
    private async Task OpenRecentProjectAsync(RecentFile? project)
    {
        if (project is null) return;
        IsLoading = true;
        try
        {
            await _workspace.OpenAsync(project.Path);
            _navigation.Navigate(typeof(Views.Pages.TimelinePage));
            _snackbar.Show("Projekt geladen", project.FileName, ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            _snackbar.Show("Öffnen fehlgeschlagen", ex.Message, ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(4));
        }
        finally { IsLoading = false; }
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

    [RelayCommand]
    private async Task OpenRecentAsync(RecentFile? file)
    {
        if (file is not null)
            await Load(file.Path);
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

public sealed record RecentFile(string Path, string FileName);
