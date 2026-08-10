namespace Singola.Avalonia.Platform;

public interface IFileDialogService
{
    Task<IReadOnlyList<string>> OpenFilesAsync(FileDialogOptions options, CancellationToken cancellationToken = default);
}

public sealed record FileDialogOptions(string Title, bool AllowMultiple = false, IReadOnlyList<string>? Extensions = null);
