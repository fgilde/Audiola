using Audiola.Controls;
using Audiola.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

namespace Audiola.Avalonia.Platform;

/// <summary>Zugriff auf das aktive Fenster — Dialoge brauchen einen Besitzer.</summary>
internal static class HostWindow
{
    public static Window? Main =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    /// <summary>Das oberste modale Fenster, sonst das Hauptfenster.</summary>
    public static Window? Active
    {
        get
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime life)
                return null;
            return life.Windows.LastOrDefault(w => w.IsActive) ?? life.MainWindow;
        }
    }
}

/// <summary>Snackbar-Meldungen über den <see cref="SnackbarHost"/> der Shell.</summary>
public sealed class AvaloniaNotifier : INotifier
{
    private SnackbarHost? _host;

    /// <summary>Vom Hauptfenster einmalig gesetzt.</summary>
    public void SetHost(SnackbarHost host) => _host = host;

    public void Success(string title, string message, int seconds = 3)
        => Show(SnackbarKind.Success, title, message, seconds);

    public void Info(string title, string message, int seconds = 3)
        => Show(SnackbarKind.Info, title, message, seconds);

    public void Warning(string title, string message, int seconds = 3)
        => Show(SnackbarKind.Warning, title, message, seconds);

    public void Error(string title, string message, int seconds = 4)
        => Show(SnackbarKind.Error, title, message, seconds);

    private void Show(SnackbarKind kind, string title, string message, int seconds)
        => DispatcherHelper.PostToUi(() => _host?.Show(kind, title, message, seconds));
}

/// <summary>Datei-/Ordnerauswahl über Avalonias StorageProvider (nativ auf allen Plattformen).</summary>
public sealed class AvaloniaFileDialogs : IFileDialogs
{
    public async Task<IReadOnlyList<string>> OpenFilesAsync(string title, bool allowMultiple = false,
        params FileFilter[] filters)
    {
        var storage = HostWindow.Active?.StorageProvider;
        if (storage is null) return [];

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple,
            FileTypeFilter = ToFileTypes(filters)
        });
        return [.. files.Select(f => f.Path.LocalPath)];
    }

    public async Task<string?> SaveFileAsync(string title, string? suggestedFileName = null,
        params FileFilter[] filters)
    {
        var storage = HostWindow.Active?.StorageProvider;
        if (storage is null) return null;

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            DefaultExtension = filters.FirstOrDefault(f => f.Extensions.Length > 0)?.Extensions[0],
            FileTypeChoices = ToFileTypes(filters)
        });
        return file?.Path.LocalPath;
    }

    public async Task<string?> PickFolderAsync(string title)
    {
        var storage = HostWindow.Active?.StorageProvider;
        if (storage is null) return null;

        var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title });
        return folders.FirstOrDefault()?.Path.LocalPath;
    }

    private static List<FilePickerFileType> ToFileTypes(IReadOnlyList<FileFilter> filters) =>
    [
        .. filters.Select(f => new FilePickerFileType(f.Name)
        {
            // Leere Endungsliste = „Alle Dateien".
            Patterns = f.Extensions.Length == 0 ? ["*"] : [.. f.Extensions.Select(e => "*." + e)]
        })
    ];
}

/// <summary>Theme-Umschaltung über Avalonias Varianten (die Dictionaries liegen in DawTheme.axaml).</summary>
public sealed class AvaloniaAppTheme : IAppTheme
{
    public bool IsLight { get; private set; }

    public void Apply(string? theme)
    {
        IsLight = string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase);
        if (Application.Current is { } app)
            app.RequestedThemeVariant = IsLight ? ThemeVariant.Light : ThemeVariant.Dark;
    }
}
