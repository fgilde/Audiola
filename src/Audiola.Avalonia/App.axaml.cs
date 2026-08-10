using Audiola.Avalonia.Platform;
using Audiola.Avalonia.Views;
using Audiola.Avalonia.Views.Pages;
using Audiola.Services;
using Audiola.Services.Audio;
using Audiola.ViewModels;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Singola.Avalonia.Platform;

namespace Audiola.Avalonia;

public partial class App : Application
{
    private ServiceProvider? _services;

    /// <summary>Per Doppelklick/Kommandozeile übergebene Datei (von Program.Main gesetzt).</summary>
    public static string? StartupFile { get; set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        _services = BuildServices();

        // Geteilten Code an den Avalonia-Host binden (UI-Thread, Fehlerdialog, Dienstzugriff).
        DispatcherHelper.Configure(
            () => Dispatcher.UIThread.CheckAccess(),
            action => Dispatcher.UIThread.Invoke(action),
            action => Dispatcher.UIThread.Post(action));
        UiError.Configure((title, message) =>
            _services!.GetRequiredService<INotifier>().Error(title, message, 8));
        AppServices.Configure(_services);

        // Gespeichertes Theme (Light/Dark) anwenden.
        _services.GetRequiredService<IAppTheme>().Apply(
            _services.GetRequiredService<ISettingsService>().Current.Theme);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = _services.GetRequiredService<MainWindow>();
            window.PendingStartupFile = StartupFile;
            desktop.MainWindow = window;
            desktop.Exit += (_, _) =>
            {
                _services?.GetService<ISettingsService>()?.Save();
                _services?.GetService<IAudioPlayerService>()?.Dispose();
                _services?.GetService<StemMixerEngine>()?.Dispose();
                _services?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Dieselbe Dienst-Landschaft wie die WPF-App — nur die UI-Verträge sind Avalonia-eigen.</summary>
    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        // Navigation / UI-Infrastruktur.
        services.AddSingleton<AvaloniaShellNavigation>(sp => new AvaloniaShellNavigation(sp));
        services.AddSingleton<IShellNavigation>(sp => sp.GetRequiredService<AvaloniaShellNavigation>());
        services.AddSingleton<AvaloniaNotifier>();
        services.AddSingleton<INotifier>(sp => sp.GetRequiredService<AvaloniaNotifier>());
        services.AddSingleton<IFileDialogs, AvaloniaFileDialogs>();
        services.AddSingleton<IAppDialogs, AvaloniaAppDialogs>();
        services.AddSingleton<IAppTheme, AvaloniaAppTheme>();

        // Plattform-Audio (miniaudio: Windows/macOS/Linux).
        services.AddSingleton<MiniAudioPlatform>();
        services.AddSingleton<IAudioPlatform>(sp => sp.GetRequiredService<MiniAudioPlatform>());

        // Eigene Dienste (identisch zur WPF-App).
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IWaveformService, WaveformService>();
        services.AddSingleton<ITrackLoader, TrackLoaderService>();
        services.AddSingleton<LiveEqProcessor>();
        services.AddSingleton<LiveFxProcessor>();
        services.AddSingleton<LiveMasterProcessor>();
        services.AddSingleton<IAudioPlayerService, NAudioPlayerService>();
        services.AddSingleton<IStemSeparationService, DemucsStemSeparationService>();
        services.AddSingleton<IAdvancedSeparationService, AudioSeparatorService>();
        services.AddSingleton<IVoiceChangeService, ElevenLabsVoiceChangeService>();
        services.AddSingleton<IPythonEnvironment, PythonEnvironmentService>();
        services.AddSingleton<ILocalVoiceService, PythonLocalVoiceService>();
        services.AddSingleton<IVoiceProfileStore, VoiceProfileStore>();
        services.AddSingleton<IAudioRecorder, AudioRecorder>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<ProjectWorkspace>();
        services.AddSingleton<UpdateService>();

        // Metadaten / Tags + einheitlicher Export.
        services.AddSingleton<IMetadataService, MetadataService>();
        services.AddSingleton<SongMetadata>();
        services.AddSingleton<ExportService>();
        services.AddSingleton(_ => new FilePreviewHost("https://www.mudex.org/preview-file"));

        // Audio-Variations-Provider.
        services.AddSingleton<IAudioVariationProvider, StudioEffectsVariationProvider>();
        services.AddSingleton<IAudioVariationProvider, ComplexManipulationVariationProvider>();
        services.AddSingleton<IAudioVariationProvider, AudioHumanizationProvider>();

        services.AddSingleton<IStemMixService, StemMixService>();
        services.AddSingleton<StemMixerEngine>();
        services.AddSingleton<IMasteringService, MasteringService>();
        services.AddSingleton<IVariationService, VariationService>();
        services.AddSingleton<IProvenanceService, ProvenanceService>();
        services.AddSingleton<IEvaluationService, EvaluationService>();

        // Geteilter Sitzungszustand (aktueller Track / Stems).
        services.AddSingleton<SessionState>();

        // Hauptfenster + ViewModels.
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<TransportViewModel>();

        // Seiten + ViewModels.
        services.AddSingleton<HomePage>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<EqualizerPage>();
        services.AddSingleton<MasteringPage>();
        services.AddSingleton<AboutPage>();
        services.AddSingleton<VariationPage>();
        services.AddSingleton<ProvenancePage>();
        services.AddSingleton<MetadataPage>();
        services.AddSingleton<EvaluationPage>();
        services.AddSingleton<SettingsPage>();
        services.AddSingleton<EditorPage>();
        services.AddSingleton<SpatialAudioPage>();
        services.AddSingleton<VoicesPage>();
        services.AddSingleton<TimelinePage>();
        services.AddSingleton<EditorViewModel>();
        services.AddSingleton<TimelineViewModel>();
        services.AddSingleton<EqualizerViewModel>();
        services.AddSingleton<MasteringViewModel>();
        services.AddSingleton<SpatialPreviewEngine>();
        services.AddSingleton<SpatialAudioViewModel>();
        services.AddSingleton<VoicesViewModel>();
        services.AddSingleton<VariationViewModel>();
        services.AddSingleton<ProvenanceViewModel>();
        services.AddSingleton<EvaluationViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MetadataViewModel>();

        // Noch nicht portierte Seiten teilen sich eine Hinweis-Ansicht.
        services.AddTransient<PendingPage>();

        // Einsing-Studio (frischer Zustand pro Aufruf).
        services.AddTransient<SingAlongViewModel>();
        services.AddTransient<SetupWizardViewModel>();

        return services.BuildServiceProvider();
    }
}
