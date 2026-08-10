using Audiola.Avalonia.Platform;
using Audiola.Avalonia.ViewModels;
using Audiola.Avalonia.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Audiola.Services;
using Audiola.Services.Audio;
using Microsoft.Extensions.DependencyInjection;
using Singola.Avalonia.Platform;

namespace Audiola.Avalonia;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFileDialogService, AvaloniaFileDialogService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IWaveformService, WaveformService>();
        services.AddSingleton<IMasteringService, MasteringService>();
        services.AddSingleton<IAudioVariationProvider, StudioEffectsVariationProvider>();
        services.AddSingleton<IStemSeparationService, DemucsStemSeparationService>();
        services.AddSingleton<IPythonEnvironment, PythonEnvironmentService>();
        services.AddSingleton<ILocalVoiceService, PythonLocalVoiceService>();
        services.AddSingleton<IVoiceProfileStore, VoiceProfileStore>();
        services.AddSingleton<IEvaluationService, EvaluationService>();
        services.AddSingleton<IProvenanceService, ProvenanceService>();
        services.AddSingleton<MiniAudioPlatform>();
        services.AddSingleton<IAudioPlatform>(provider => provider.GetRequiredService<MiniAudioPlatform>());
        services.AddSingleton<AudiolaHostViewModel>();
        services.AddSingleton<MainWindow>();
        _services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = _services.GetRequiredService<MainWindow>();
            _services.GetRequiredService<AvaloniaFileDialogService>().SetOwner(window);
            desktop.MainWindow = window;
            desktop.Exit += (_, _) => _services?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
