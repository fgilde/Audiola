namespace Audiola.Avalonia.Platform;

public interface IFileDialogService
{
    Task<IReadOnlyList<string>> OpenFilesAsync(FileDialogOptions options, CancellationToken cancellationToken = default);
    Task<string?> SaveFileAsync(FileDialogOptions options, CancellationToken cancellationToken = default);
}

public sealed record FileDialogOptions(
    string Title,
    bool AllowMultiple = false,
    string SuggestedFileName = "",
    IReadOnlyList<string>? Extensions = null);
