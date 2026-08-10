using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Singola.Avalonia.Platform;

public sealed class AvaloniaFileDialogService : IFileDialogService
{
    private TopLevel? _owner;

    public void SetOwner(TopLevel owner) => _owner = owner;

    public async Task<IReadOnlyList<string>> OpenFilesAsync(FileDialogOptions options, CancellationToken cancellationToken = default)
    {
        var owner = _owner ?? throw new InvalidOperationException("The desktop window has not been initialized.");
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = options.Title,
            AllowMultiple = options.AllowMultiple,
            FileTypeFilter = options.Extensions is { Count: > 0 }
                ? [new FilePickerFileType("Supported songs") { Patterns = options.Extensions.Select(extension => $"*.{extension.TrimStart('.')}").ToArray() }]
                : null
        });
        return files.Select(file => file.TryGetLocalPath() ?? file.Name).ToArray();
    }

    public async Task<string?> SaveFileAsync(SaveDialogOptions options, CancellationToken cancellationToken = default)
    {
        var owner = _owner ?? throw new InvalidOperationException("The desktop window has not been initialized.");
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = options.Title,
            SuggestedFileName = options.SuggestedName,
            DefaultExtension = options.Extensions.FirstOrDefault(),
            ShowOverwritePrompt = true,
            // Ein Eintrag pro Format, damit die Endung im Dialog wählbar ist und der Exporter
            // daran das Ziel-Codec erkennt.
            FileTypeChoices = [.. options.Extensions.Select(extension =>
                new FilePickerFileType(extension.ToUpperInvariant()) { Patterns = [$"*.{extension}"] })]
        });
        return file?.TryGetLocalPath();
    }
}
