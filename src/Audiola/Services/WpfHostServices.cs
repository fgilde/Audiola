using System.Windows;
using Audiola.Models;
using Audiola.ViewModels;
using Audiola.Views.Dialogs;
using Microsoft.Win32;
using Wpf.Ui;

namespace Audiola.Services;

/// <summary>Snackbar-Meldungen über den WPF-UI-Dienst.</summary>
public sealed class WpfNotifier(ISnackbarService snackbar) : INotifier
{
    public void Success(string title, string message, int seconds = 3) => snackbar.Success(title, message, seconds);
    public void Info(string title, string message, int seconds = 3) => snackbar.Info(title, message, seconds);
    public void Warning(string title, string message, int seconds = 3) => snackbar.Warning(title, message, seconds);
    public void Error(string title, string message, int seconds = 4) => snackbar.Error(title, message, seconds);
}

/// <summary>Datei-/Ordnerauswahl über die Windows-Standarddialoge.</summary>
public sealed class WpfFileDialogs : IFileDialogs
{
    public Task<IReadOnlyList<string>> OpenFilesAsync(string title, bool allowMultiple = false,
        params FileFilter[] filters)
    {
        var dialog = new OpenFileDialog { Title = title, Multiselect = allowMultiple, Filter = ToFilter(filters) };
        IReadOnlyList<string> result = dialog.ShowDialog() == true ? dialog.FileNames : [];
        return Task.FromResult(result);
    }

    public Task<string?> SaveFileAsync(string title, string? suggestedFileName = null, params FileFilter[] filters)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = ToFilter(filters),
            FileName = suggestedFileName ?? ""
        };
        return Task.FromResult(dialog.ShowDialog() == true ? dialog.FileName : null);
    }

    public Task<string?> PickFolderAsync(string title)
    {
        var dialog = new OpenFolderDialog { Title = title };
        return Task.FromResult(dialog.ShowDialog() == true ? dialog.FolderName : null);
    }

    /// <summary>Baut die Windows-Filterzeile ("Name|*.a;*.b|…") aus den host-neutralen Filtern.</summary>
    private static string ToFilter(IReadOnlyList<FileFilter> filters)
    {
        if (filters.Count == 0) return "Alle Dateien|*.*";
        return string.Join('|', filters.Select(f => f.Extensions.Length == 0
            ? $"{f.Name}|*.*"
            : $"{f.Name}|{string.Join(';', f.Extensions.Select(e => "*." + e))}"));
    }
}

/// <summary>Eigene WPF-Fenster, die geteilter Code anfordert.</summary>
public sealed class WpfAppDialogs : IAppDialogs
{
    private static Window? Owner =>
        Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        ?? Application.Current?.MainWindow;

    public void ShowTrackMastering(object trackViewModel)
    {
        if (trackViewModel is not StemTrackViewModel track) return;
        new TrackMasteringDialog(AppServices.Get<MasteringViewModel>(), track)
        {
            Owner = Application.Current?.MainWindow
        }.ShowDialog();
    }

    public void OpenSingAlong()
        => new SingAlongWindow(AppServices.Get<SingAlongViewModel>())
        {
            Owner = Application.Current?.MainWindow
        }.Show();   // eigenes Fenster, nicht-modal – kann neben dem Studio liegen

    public bool Confirm(string title, string message)
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
           == MessageBoxResult.Yes;

    public async Task<SaveDiscardCancel> AskSaveDiscardCancelAsync(string title, string message)
    {
        var box = new Wpf.Ui.Controls.MessageBox
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Speichern",
            SecondaryButtonText = "Verwerfen",
            CloseButtonText = "Abbrechen"
        };
        return await box.ShowDialogAsync() switch
        {
            Wpf.Ui.Controls.MessageBoxResult.Primary => SaveDiscardCancel.Save,
            Wpf.Ui.Controls.MessageBoxResult.Secondary => SaveDiscardCancel.Discard,
            _ => SaveDiscardCancel.Cancel
        };
    }

    public void ShowSetupWizard()
    {
        var wizard = AppServices.Get<SetupWizardWindow>();
        wizard.Owner = Application.Current?.MainWindow;
        wizard.ShowDialog();
    }

    public Task<ExportRequest?> ShowExportAsync(ExportDialogRequest request)
    {
        var dialog = new ExportDialog(request.DefaultFileName, request.Seed, request.SeedLyrics,
            request.GenerateLyrics, request.ElevenLabsAvailable, request.PreviewAsync)
        {
            Owner = Application.Current?.MainWindow
        };
        return Task.FromResult(dialog.ShowDialog() == true ? dialog.Result : null);
    }

    public Task ShowFilePreviewAsync(string url, string fileName)
    {
        new PreviewDialog(url, fileName) { Owner = Owner }.ShowDialog();
        return Task.CompletedTask;
    }
}

/// <summary>Theme-Umschaltung über den WPF-UI-<see cref="ThemeManager"/>.</summary>
public sealed class WpfAppTheme : IAppTheme
{
    public bool IsLight => ThemeManager.IsLight;

    public void Apply(string? theme) => ThemeManager.Apply(theme);
}
