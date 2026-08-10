using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Singola.Avalonia.Platform;

namespace Singola.Avalonia.ViewModels;

public sealed partial class SingolaHostViewModel : ObservableObject
{
    private readonly IFileDialogService _fileDialogs;

    [ObservableProperty]
    private string _songStatus = "Choose a song to begin.";

    public SingolaHostViewModel(IFileDialogService fileDialogs) => _fileDialogs = fileDialogs;

    [RelayCommand]
    private async Task ChooseSongAsync()
    {
        var songs = await _fileDialogs.OpenFilesAsync(
            new FileDialogOptions("Open song", Extensions: ["mp3", "wav", "flac", "m4a", "ogg"]));
        SongStatus = songs.Count == 0 ? "Choose a song to begin." : Path.GetFileName(songs[0]);
    }
}
