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
}
