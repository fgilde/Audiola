using System.IO;
using System.Windows;
using System.Windows.Threading;
using Audiola.Services;
using Audiola.ViewModels;
using Audiola.Views;
using Audiola.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui;

namespace Audiola;

public partial class App : Application
{
    private static readonly IHost Host = Microsoft.Extensions.Hosting.Host
        .CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {
            // Navigation / UI-Infrastruktur (WPF-UI 3.0.5).
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ISnackbarService, SnackbarService>();
            services.AddSingleton<IContentDialogService, ContentDialogService>();

            // Eigene Dienste.
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IWaveformService, WaveformService>();
            services.AddSingleton<ITrackLoader, TrackLoaderService>();
            services.AddSingleton<LiveEqProcessor>();
            services.AddSingleton<LiveFxProcessor>();
            services.AddSingleton<LiveMasterProcessor>();
            services.AddSingleton<IAudioPlayerService, NAudioPlayerService>();
            services.AddSingleton<IStemSeparationService, DemucsStemSeparationService>();
            services.AddSingleton<IVoiceChangeService, ElevenLabsVoiceChangeService>();
            services.AddSingleton<IPythonEnvironment, PythonEnvironmentService>();
            services.AddSingleton<ILocalVoiceService, PythonLocalVoiceService>();
            services.AddSingleton<IVoiceProfileStore, VoiceProfileStore>();
            services.AddSingleton<IAudioRecorder, AudioRecorder>();
            services.AddSingleton<IProjectService, ProjectService>();
            services.AddSingleton<ProjectWorkspace>();

            // Audio-Variations-Provider (weitere Implementierungen hier registrieren):
            services.AddSingleton<IAudioVariationProvider, StudioEffectsVariationProvider>();
            services.AddSingleton<IAudioVariationProvider, ComplexManipulationVariationProvider>();
            services.AddSingleton<IAudioVariationProvider, AudioHumanizationProvider>();

            var pythonDetectorProvider = new PythonAudioDetectorProbeProvider(pythonExe: "python", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio_detector_probe.py"));
            if(pythonDetectorProvider.ScriptExists)
                services.AddSingleton<IAudioVariationProvider>(pythonDetectorProvider);
            
            services.AddSingleton<IStemMixService, StemMixService>();
            services.AddSingleton<StemMixerEngine>();
            services.AddSingleton<IMasteringService, MasteringService>();
            services.AddSingleton<IVariationService, VariationService>();
            services.AddSingleton<IProvenanceService, ProvenanceService>();
            services.AddSingleton<IEvaluationService, EvaluationService>();

            // Geteilter Sitzungszustand (aktueller Track / Stems).
            services.AddSingleton<SessionState>();

            // Hauptfenster + ViewModel.
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<TransportViewModel>();

            // Seiten + ViewModels.
            services.AddSingleton<HomePage>();
            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<EditorPage>();
            services.AddSingleton<EditorViewModel>();
            services.AddSingleton<TimelinePage>();
            services.AddSingleton<TimelineViewModel>();
            services.AddSingleton<EqualizerPage>();
            services.AddSingleton<EqualizerViewModel>();
            services.AddSingleton<MasteringPage>();
            services.AddSingleton<MasteringViewModel>();
            services.AddSingleton<SpatialPreviewEngine>();
            services.AddSingleton<SpatialAudioPage>();
            services.AddSingleton<SpatialAudioViewModel>();
            services.AddSingleton<VoicesPage>();
            services.AddSingleton<VoicesViewModel>();
            services.AddSingleton<VariationPage>();
            services.AddSingleton<VariationViewModel>();
            services.AddSingleton<ProvenancePage>();
            services.AddSingleton<ProvenanceViewModel>();
            services.AddSingleton<EvaluationPage>();
            services.AddSingleton<EvaluationViewModel>();
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<SettingsViewModel>();
        })
        .Build();

    public static IServiceProvider Services => Host.Services;

    public static T GetService<T>() where T : class
        => Host.Services.GetService(typeof(T)) as T
           ?? throw new InvalidOperationException($"Dienst {typeof(T)} nicht registriert.");

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
        base.OnStartup(e);
        await Host.StartAsync();

        // Studio-Akzentfarbe.
        Wpf.Ui.Appearance.ApplicationAccentColorManager.Apply(
            System.Windows.Media.Color.FromRgb(0x5B, 0x8C, 0xFF),
            Wpf.Ui.Appearance.ApplicationTheme.Dark,
            false);

        var window = GetService<MainWindow>();
        window.Show();
        }
        catch (Exception ex)
        {
            try { File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "audiola.log"),
                $"[{DateTimeOffset.UtcNow:O}] [OnStartup] {ex}\n\n"); } catch { }
            throw;
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        GetService<ISettingsService>().Save();
        GetService<IAudioPlayerService>().Dispose();
        GetService<StemMixerEngine>().Dispose();

        await Host.StopAsync();
        Host.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Platzhalter fuer zentrales Fehler-Logging.
    }
}
