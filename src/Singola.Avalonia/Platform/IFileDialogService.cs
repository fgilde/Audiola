namespace Singola.Avalonia.Platform;

public interface IFileDialogService
{
    Task<IReadOnlyList<string>> OpenFilesAsync(FileDialogOptions options, CancellationToken cancellationToken = default);

    /// <summary>Asks where to write a file; returns <c>null</c> when the dialog was cancelled.</summary>
    Task<string?> SaveFileAsync(SaveDialogOptions options, CancellationToken cancellationToken = default);
}

public sealed record FileDialogOptions(string Title, bool AllowMultiple = false, IReadOnlyList<string>? Extensions = null);

public sealed record SaveDialogOptions(string Title, string SuggestedName, IReadOnlyList<string> Extensions);
