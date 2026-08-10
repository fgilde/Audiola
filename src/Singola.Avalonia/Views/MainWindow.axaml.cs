using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

    /// <summary>Song aus der Kommandozeile („Öffnen mit …“), geladen sobald das Fenster steht.</summary>
    public string? PendingStartupFile { get; set; }

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

        // Songs lassen sich auf das Fenster ziehen — genau wie in der Windows-Fassung.
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        Opened += async (_, _) =>
        {
            if (PendingStartupFile is not { } file) return;
            PendingStartupFile = null;
            await _viewModel.LoadSongAsync(file);
        };
        Closed += (_, _) =>
        {
            _timer.Stop();
            _viewModel.Engine.Dispose();
        };
    }

    private async void ChooseSong_Click(object? sender, RoutedEventArgs e)
    {
        var songs = await _fileDialogs!.OpenFilesAsync(new FileDialogOptions(
            "Song öffnen", Extensions: [.. SongLoader.AudioExtensions.Select(extension => extension.TrimStart('.')), "audiola"]));
        if (songs.FirstOrDefault(SongLoader.IsSupported) is { } song)
            await _viewModel!.LoadSongAsync(song);
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DroppedSong(e) is null ? DragDropEffects.None : DragDropEffects.Copy;
        e.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        if (DroppedSong(e) is { } song && _viewModel is not null) await _viewModel.LoadSongAsync(song);
    }

    private static string? DroppedSong(DragEventArgs e) => e.Data.GetFiles()?
        .Select(file => file.TryGetLocalPath())
        .FirstOrDefault(path => path is not null && SongLoader.IsSupported(path));
}
