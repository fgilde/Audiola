using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Singola.Avalonia.Platform;
using Singola.Services;
using Singola.ViewModels;

namespace Singola.Avalonia.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel? _viewModel;
    private readonly IFileDialogService? _fileDialogs;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(50) };

    public MainWindow() => InitializeComponent();

    public MainWindow(MainViewModel viewModel, IFileDialogService fileDialogs)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _fileDialogs = fileDialogs;
        Stage.Attach(viewModel);
        _timer.Tick += (_, _) => _viewModel.Tick();
        _timer.Start();
        Closed += (_, _) =>
        {
            _timer.Stop();
            _viewModel.Engine.Dispose();
        };
    }

    private async void ChooseSong_Click(object? sender, RoutedEventArgs e)
    {
        var songs = await _fileDialogs!.OpenFilesAsync(new FileDialogOptions(
            "Open song", Extensions: [.. SongLoader.AudioExtensions.Select(extension => extension.TrimStart('.')), "audiola"]));
        if (songs.FirstOrDefault(SongLoader.IsSupported) is { } song)
            await _viewModel!.LoadSongAsync(song);
    }
}
