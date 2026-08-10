using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Singola.Avalonia.Platform;
using Singola.Avalonia.Views;

namespace Singola.Avalonia;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AvaloniaFileDialogService>();
        services.AddSingleton<IFileDialogService>(provider => provider.GetRequiredService<AvaloniaFileDialogService>());
        services.AddSingleton<MiniAudioPlatform>();
        services.AddSingleton<Audiola.Services.Audio.IAudioPlatform>(provider =>
            provider.GetRequiredService<MiniAudioPlatform>());
        services.AddSingleton<Singola.ViewModels.MainViewModel>();
        services.AddSingleton<MainWindow>();
        _services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = _services.GetRequiredService<MainWindow>();
            _services.GetRequiredService<AvaloniaFileDialogService>().SetOwner(window);
            // „Öffnen mit …“ bzw. Song als Startargument — wie in der Windows-Fassung.
            window.PendingStartupFile = desktop.Args?.FirstOrDefault(File.Exists);
            desktop.MainWindow = window;
            desktop.Exit += (_, _) => _services?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
