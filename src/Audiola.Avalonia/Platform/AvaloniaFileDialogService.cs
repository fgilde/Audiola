using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Audiola.Avalonia.Platform;

public sealed class AvaloniaFileDialogService : IFileDialogService
{
    private TopLevel? _owner;

    public void SetOwner(TopLevel owner) => _owner = owner;

    public async Task<IReadOnlyList<string>> OpenFilesAsync(FileDialogOptions options, CancellationToken cancellationToken = default)
    {
        var owner = GetOwner();
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = options.Title,
            AllowMultiple = options.AllowMultiple,
            FileTypeFilter = CreateFileTypes(options.Extensions)
        });

        return files.Select(file => file.TryGetLocalPath() ?? file.Name).ToArray();
    }

    public async Task<string?> SaveFileAsync(FileDialogOptions options, CancellationToken cancellationToken = default)
    {
        var file = await GetOwner().StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = options.Title,
            SuggestedFileName = options.SuggestedFileName,
            FileTypeChoices = CreateFileTypes(options.Extensions)
        });

        return file?.TryGetLocalPath() ?? file?.Name;
    }

    private TopLevel GetOwner() => _owner ?? throw new InvalidOperationException("The desktop window has not been initialized.");

    private static IReadOnlyList<FilePickerFileType>? CreateFileTypes(IReadOnlyList<string>? extensions) =>
        extensions is { Count: > 0 }
            ? [new FilePickerFileType("Supported files") { Patterns = extensions.Select(extension => $"*.{extension.TrimStart('.')}").ToArray() }]
            : null;
}
