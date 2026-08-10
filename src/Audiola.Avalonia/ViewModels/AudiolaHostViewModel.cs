using System.Collections.ObjectModel;
using System.Reflection;
using Audiola.Avalonia.Platform;
using Audiola.Models;
using Audiola.Services;
using Audiola.Services.Audio;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Audiola.Avalonia.ViewModels;

public sealed partial class AudiolaHostViewModel : ObservableObject, IDisposable
{
    private static readonly string[] AudioExtensions = ["mp3", "wav", "flac", "m4a", "ogg", "aiff", "aac"];
    private readonly IFileDialogService _fileDialogs;
    private readonly IProjectService _projects;
    private readonly IWaveformService _waveforms;
    private readonly ISettingsService _settings;
    private readonly IMasteringService _mastering;
    private readonly IAudioVariationProvider _variations;
    private readonly IStemSeparationService _stemSeparator;
    private readonly ILocalVoiceService _voices;
    private readonly IVoiceProfileStore _voiceProfiles;
    private readonly IEvaluationService _evaluation;
    private readonly IProvenanceService _provenance;
    private readonly IAudioPlayback _playback;
    private readonly DispatcherTimer _playbackTimer;
    private readonly AudioMetadata _metadata = new();
    private IReadOnlyList<EvalRow> _evaluationRows = [];
    private string? _openedPlaybackPath;
    private bool _updatingPlaybackProgress;

    public AudiolaHostViewModel(
        IFileDialogService fileDialogs,
        IProjectService projects,
        IWaveformService waveforms,
        ISettingsService settings,
        IMasteringService mastering,
        IAudioVariationProvider variations,
        IStemSeparationService stemSeparator,
        ILocalVoiceService voices,
        IVoiceProfileStore voiceProfiles,
        IEvaluationService evaluation,
        IProvenanceService provenance,
        IAudioPlatform audio)
    {
        _fileDialogs = fileDialogs;
        _projects = projects;
        _waveforms = waveforms;
        _settings = settings;
        _mastering = mastering;
        _variations = variations;
        _stemSeparator = stemSeparator;
        _voices = voices;
        _voiceProfiles = voiceProfiles;
        _evaluation = evaluation;
        _provenance = provenance;
        _playback = audio.CreatePlayback();
        _playback.PlaybackEnded += OnPlaybackEnded;
        _playbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _playbackTimer.Tick += OnPlaybackTimerTick;

        foreach (var variation in _variations.GetVariations())
            Variations.Add(variation);
        SelectedVariation = Variations.FirstOrDefault();
        foreach (var profile in Audiola.Models.MasteringProfiles.All)
            MasteringProfiles.Add(profile.Name);
        SelectedMasteringProfile = MasteringProfiles.FirstOrDefault();
        PythonPath = settings.Current.PythonPath;
        DemucsModel = settings.Current.DemucsModel;
        StemOutputDirectory = settings.Current.OutputDirectory;
        RefreshRecents();
        RefreshVoiceProfiles();

        Pages =
        [
            new NavigationItem("Home", "⌂", new HomePageViewModel(this)),
            new NavigationItem("Editor", "✂", new EditorPageViewModel(this)),
            new NavigationItem("Timeline", "≡", new TimelinePageViewModel(this)),
            new NavigationItem("Equalizer", "≋", new EqualizerPageViewModel(this)),
            new NavigationItem("Mastering", "◆", new MasteringPageViewModel(this)),
            new NavigationItem("Spatial Audio", "◉", new SpatialAudioPageViewModel(this)),
            new NavigationItem("Voices", "♬", new VoicesPageViewModel(this)),
            new NavigationItem("Variations", "✦", new VariationsPageViewModel(this)),
            new NavigationItem("Provenance", "⌕", new ProvenancePageViewModel(this)),
            new NavigationItem("Evaluation", "✓", new EvaluationPageViewModel(this)),
            new NavigationItem("Metadata", "⌑", new MetadataPageViewModel(this)),
            new NavigationItem("Settings", "⚙", new SettingsPageViewModel(this)),
            new NavigationItem("About", "ⓘ", new AboutPageViewModel(this))
        ];
        SelectedPage = Pages[0];
    }

    public string ApplicationTitle => "Audiola";
    public string VersionText => $"Avalonia host · {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "development"}";
    public string PlatformText => $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription} · {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}";
    public IReadOnlyList<NavigationItem> Pages { get; }
    public ObservableCollection<ProjectTrackItem> Tracks { get; } = [];
    public ObservableCollection<RecentItem> RecentFiles { get; } = [];
    public ObservableCollection<RecentItem> RecentProjects { get; } = [];
    public ObservableCollection<AudioVariation> Variations { get; } = [];
    public ObservableCollection<string> MasteringProfiles { get; } = [];
    public ObservableCollection<SpatialSourceItem> SpatialSources { get; } = [];
    public ObservableCollection<LocalVoiceModel> VoiceModels { get; } = [];
    public ObservableCollection<VoiceProfile> VoiceProfiles { get; } = [];
    public ObservableCollection<Finding> ProvenanceFindings { get; } = [];

    [ObservableProperty] private NavigationItem? _selectedPage;
    [ObservableProperty] private ProjectTrackItem? _selectedTrack;
    [ObservableProperty] private AudioTrack? _activeTrack;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private double _playbackProgress;
    [ObservableProperty] private string _playbackPositionText = "00:00";
    [ObservableProperty] private string _playbackDurationText = "00:00";
    [ObservableProperty] private string _status = "Ready";
    [ObservableProperty] private bool _isWorking;
    [ObservableProperty] private string _projectName = "Untitled project";
    [ObservableProperty] private AudioVariation? _selectedVariation;
    [ObservableProperty] private string? _selectedMasteringProfile;
    [ObservableProperty] private string _masteringStatus = "Load audio to measure, preview, or export a master.";
    [ObservableProperty] private string _spatialStatus = "Add audio tracks to position them in a 3D scene.";
    [ObservableProperty] private string _voiceStatus = "Loading local voice models is optional.";
    [ObservableProperty] private VoiceProfile? _selectedVoiceProfile;
    [ObservableProperty] private string _voiceText = "Hello from Audiola.";
    [ObservableProperty] private double _voiceSpeed = 1;
    [ObservableProperty] private string _provenanceStatus = "Analyze an audio file for embedded provenance markers.";
    [ObservableProperty] private string _provenanceAssessment = "";
    [ObservableProperty] private string _provenanceRaw = "";
    [ObservableProperty] private string _evaluationStatus = "Load a labeled CSV with truth/label and predicted/verdict or score.";
    [ObservableProperty] private bool _evaluationHasScores;
    [ObservableProperty] private bool _evaluationHasResult;
    [ObservableProperty] private double _evaluationThreshold = .5;
    [ObservableProperty] private int _truePositives;
    [ObservableProperty] private int _falsePositives;
    [ObservableProperty] private int _trueNegatives;
    [ObservableProperty] private int _falseNegatives;
    [ObservableProperty] private int _evaluationSkipped;
    [ObservableProperty] private string _accuracy = "—";
    [ObservableProperty] private string _precision = "—";
    [ObservableProperty] private string _recall = "—";
    [ObservableProperty] private string _f1 = "—";
    [ObservableProperty] private string _specificity = "—";
    [ObservableProperty] private string _falsePositiveRate = "—";
    [ObservableProperty] private string _falseNegativeRate = "—";
    [ObservableProperty] private string _metadataTitle = "";
    [ObservableProperty] private string _metadataArtist = "";
    [ObservableProperty] private string _metadataAlbum = "";
    [ObservableProperty] private string _metadataGenre = "";
    [ObservableProperty] private string _metadataLyrics = "";
    [ObservableProperty] private string _metadataCoverStatus = "No cover artwork selected";
    [ObservableProperty] private string _pythonPath = "";
    [ObservableProperty] private string _demucsModel = "";
    [ObservableProperty] private string _stemOutputDirectory = "";
    [ObservableProperty] private string _settingsStatus = "";
    [ObservableProperty] private double _targetLufs = -14;
    [ObservableProperty] private bool _highPassEnabled = true;
    [ObservableProperty] private double _highPassHz = 30;
    [ObservableProperty] private bool _compressorEnabled = true;
    [ObservableProperty] private double _thresholdDb = -18;
    [ObservableProperty] private double _ratio = 2;
    [ObservableProperty] private double _lowShelfGainDb;
    [ObservableProperty] private double _midGainDb;
    [ObservableProperty] private double _highShelfGainDb;
    [ObservableProperty] private bool _normalizeLoudness = true;

    public bool HasAudio => ActiveTrack is not null;
    public bool IsTransportAvailable => HasAudio;
    public string PlayPauseLabel => IsPlaying ? "Pause" : "Play";
    public bool HasTracks => Tracks.Count > 0;
    public bool VoiceEngineAvailable => _voices.ScriptAvailable;
    public bool CanSynthesizeVoice => VoiceEngineAvailable && SelectedVoiceProfile is not null && !string.IsNullOrWhiteSpace(VoiceText);
    public string VoiceEngineMessage => VoiceEngineAvailable
        ? "Local Python voice engine is available."
        : "Local voice operations are disabled: install voicebox_engine.py beside Audiola and its configured Python packages.";
    public string SpatialPreviewMessage =>
        "The global transport plays the active source track with a live waveform playhead. Export binaural or multichannel WAV to preview a rendered spatial mix.";
    public string C2paHelp => "Full C2PA manifest output requires c2patool to be installed and available on PATH.";
    public IReadOnlyList<string> DemucsModels { get; } = ["htdemucs", "htdemucs_ft", "htdemucs_6s"];
    public IReadOnlyList<string> VoiceDevices { get; } = ["auto", "cuda", "cpu", "directml"];

    partial void OnSelectedPageChanged(NavigationItem? value)
    {
        foreach (var page in Pages)
            page.IsSelected = ReferenceEquals(page, value);
        if (value?.Content is SpatialAudioPageViewModel)
            EnsureSpatialSources();
        if (value?.Content is VoicesPageViewModel && VoiceModels.Count == 0)
            _ = LoadVoiceModelsAsync();
    }

    partial void OnSelectedTrackChanged(ProjectTrackItem? value)
    {
        ActiveTrack = value?.Waveform;
        OnPropertyChanged(nameof(HasAudio));
    }

    partial void OnActiveTrackChanged(AudioTrack? value)
    {
        StopPlayback();
        PlaybackDurationText = FormatTime(value?.Duration ?? TimeSpan.Zero);
        OnPropertyChanged(nameof(HasAudio));
        OnPropertyChanged(nameof(IsTransportAvailable));
        OnPropertyChanged(nameof(HasTracks));
        ApplyVariationCommand.NotifyCanExecuteChanged();
        ExportMasterCommand.NotifyCanExecuteChanged();
        SeparateTrackCommand.NotifyCanExecuteChanged();
        AnalyzeCurrentTrackCommand.NotifyCanExecuteChanged();
        PlayPauseCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsPlayingChanged(bool value) => OnPropertyChanged(nameof(PlayPauseLabel));

    partial void OnPlaybackProgressChanged(double value)
    {
        if (_updatingPlaybackProgress || ActiveTrack is null) return;
        try
        {
            EnsurePlaybackOpen();
            _playback.Seek(TimeSpan.FromSeconds(ActiveTrack.Duration.TotalSeconds * Math.Clamp(value, 0, 1)));
            UpdatePlaybackState();
        }
        catch (Exception ex) { Status = $"Seek failed: {ex.Message}"; }
    }

    partial void OnEvaluationThresholdChanged(double value)
    {
        if (EvaluationHasScores && _evaluationRows.Count > 0)
            RecomputeEvaluation();
    }

    partial void OnSelectedMasteringProfileChanged(string? value)
    {
        var profile = MasteringProfilesCore().FirstOrDefault(x => x.Name == value);
        if (profile is not null)
            ApplyMasteringSettings(profile.Settings);
    }

    partial void OnSelectedVoiceProfileChanged(VoiceProfile? value) => SynthesizeVoiceCommand.NotifyCanExecuteChanged();
    partial void OnVoiceTextChanged(string value) => SynthesizeVoiceCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void Navigate(NavigationItem? page)
    {
        if (page is not null)
            SelectedPage = page;
    }

    [RelayCommand(CanExecute = nameof(HasAudio))]
    private void PlayPause()
    {
        try
        {
            EnsurePlaybackOpen();
            if (_playback.IsPlaying)
            {
                _playback.Pause();
                IsPlaying = false;
                _playbackTimer.Stop();
                UpdatePlaybackState();
                Status = "Playback paused.";
                return;
            }

            if (_playback.Duration > TimeSpan.Zero && _playback.Position >= _playback.Duration)
                _playback.Seek(TimeSpan.Zero);
            _playback.Play();
            IsPlaying = true;
            _playbackTimer.Start();
            UpdatePlaybackState();
            Status = $"Playing {ActiveTrack?.FileName}.";
        }
        catch (Exception ex)
        {
            IsPlaying = false;
            Status = $"Playback failed: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(HasAudio))]
    private void Stop()
    {
        StopPlayback();
        Status = "Playback stopped.";
    }

    [RelayCommand]
    private void Seek(double ratio)
    {
        PlaybackProgress = Math.Clamp(ratio, 0, 1);
    }

    [RelayCommand]
    private async Task OpenAudioAsync()
    {
        var files = await _fileDialogs.OpenFilesAsync(new FileDialogOptions(
            "Open audio files", AllowMultiple: true, Extensions: AudioExtensions));
        if (files.Count == 0) return;

        IsWorking = true;
        try
        {
            foreach (var file in files.Where(File.Exists))
                await AddAudioAsync(file);
            SelectedPage = Pages.First(p => p.Title == "Timeline");
            Status = $"{files.Count} audio file(s) imported.";
        }
        catch (Exception ex)
        {
            Status = $"Audio import failed: {ex.Message}";
        }
        finally { IsWorking = false; }
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        var path = (await _fileDialogs.OpenFilesAsync(new FileDialogOptions(
            "Open Audiola project", Extensions: ["audiola"]))).FirstOrDefault();
        if (path is null) return;

        IsWorking = true;
        try
        {
            var project = await _projects.LoadAsync(path);
            await LoadProjectAsync(project);
            ProjectName = Path.GetFileNameWithoutExtension(path);
            AddRecent(_settings.Current.RecentProjects, path);
            _settings.Save();
            RefreshRecents();
            SelectedPage = Pages.First(p => p.Title == "Timeline");
            Status = $"Project loaded: {Tracks.Count} track(s).";
        }
        catch (Exception ex)
        {
            Status = $"Project import failed: {ex.Message}";
        }
        finally { IsWorking = false; }
    }

    [RelayCommand(CanExecute = nameof(HasTracks))]
    private async Task SaveProjectAsync()
    {
        var suggested = $"{SanitizeFileName(ProjectName)}.audiola";
        var path = await _fileDialogs.SaveFileAsync(new FileDialogOptions(
            "Save Audiola project", SuggestedFileName: suggested, Extensions: ["audiola"]));
        if (path is null) return;
        if (!path.EndsWith(".audiola", StringComparison.OrdinalIgnoreCase)) path += ".audiola";

        IsWorking = true;
        try
        {
            ApplyMetadata();
            await _projects.SaveAsync(path, BuildProject());
            ProjectName = Path.GetFileNameWithoutExtension(path);
            AddRecent(_settings.Current.RecentProjects, path);
            _settings.Save();
            RefreshRecents();
            Status = $"Project saved: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            Status = $"Project save failed: {ex.Message}";
        }
        finally { IsWorking = false; }
    }

    [RelayCommand(CanExecute = nameof(HasTracks))]
    private void RemoveSelectedTrack()
    {
        if (SelectedTrack is null) return;
        var index = Tracks.IndexOf(SelectedTrack);
        Tracks.Remove(SelectedTrack);
        SelectedTrack = Tracks.ElementAtOrDefault(Math.Clamp(index, 0, Math.Max(0, Tracks.Count - 1)));
        EnsureSpatialSources();
        OnPropertyChanged(nameof(HasTracks));
        SaveProjectCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(HasTracks))]
    private void MoveSelectedTrackUp()
    {
        if (SelectedTrack is null) return;
        var index = Tracks.IndexOf(SelectedTrack);
        if (index > 0) Tracks.Move(index, index - 1);
    }

    [RelayCommand(CanExecute = nameof(HasAudio))]
    private async Task ApplyVariationAsync()
    {
        if (ActiveTrack is null || SelectedVariation is null) return;
        var path = await _fileDialogs.SaveFileAsync(new FileDialogOptions(
            "Export edited audio", SuggestedFileName: $"{Path.GetFileNameWithoutExtension(ActiveTrack.FileName)}-{SelectedVariation.Id}.wav",
            Extensions: ["wav", "flac", "mp3", "m4a"]));
        if (path is null) return;

        IsWorking = true;
        try
        {
            var source = ActiveTrack.FilePath;
            var variation = SelectedVariation;
            await Task.Run(async () =>
            {
                var (samples, sampleRate) = AudioProcessingHelper.ReadStereo(source);
                var output = await _variations.ApplyAsync(variation.Id, samples, sampleRate);
                AudioExporter.Export(output, sampleRate, 2, path);
            });
            await AddAudioAsync(path);
            Status = $"{variation.Name} exported and added to the timeline.";
        }
        catch (Exception ex)
        {
            Status = $"Effect export failed: {ex.Message}";
        }
        finally { IsWorking = false; }
    }

    [RelayCommand]
    private void SelectVariation(AudioVariation? variation)
    {
        if (variation is not null)
            SelectedVariation = variation;
    }

    [RelayCommand(CanExecute = nameof(HasAudio))]
    private async Task SeparateTrackAsync()
    {
        if (ActiveTrack is null) return;
        IsWorking = true;
        try
        {
            if (!await _stemSeparator.IsAvailableAsync())
            {
                Status = "Stem separation is disabled: install Demucs in the configured Python environment.";
                return;
            }

            var progress = new Progress<string>(s => Status = s);
            var set = await _stemSeparator.SeparateAsync(ActiveTrack.FilePath, progress);
            foreach (var stem in set.Stems)
                await AddAudioAsync(stem.FilePath, stem.Kind.ToString());
            EnsureSpatialSources();
            Status = $"{set.Stems.Count} stems added to the timeline.";
        }
        catch (Exception ex)
        {
            Status = $"Stem separation failed: {ex.Message}";
        }
        finally { IsWorking = false; }
    }

    [RelayCommand(CanExecute = nameof(HasAudio))]
    private async Task MeasureLoudnessAsync()
    {
        if (ActiveTrack is null) return;
        IsWorking = true;
        try
        {
            var lufs = await _mastering.MeasureLufsAsync(ActiveTrack.FilePath);
            MasteringStatus = $"{ActiveTrack.FileName}: {lufs:0.0} LUFS integrated.";
        }
        catch (Exception ex) { MasteringStatus = $"Loudness measurement failed: {ex.Message}"; }
        finally { IsWorking = false; }
    }

    [RelayCommand(CanExecute = nameof(HasAudio))]
    private async Task ExportMasterAsync()
    {
        if (ActiveTrack is null) return;
        var path = await _fileDialogs.SaveFileAsync(new FileDialogOptions(
            "Export mastered audio",
            SuggestedFileName: $"{Path.GetFileNameWithoutExtension(ActiveTrack.FileName)}-mastered.wav",
            Extensions: ["wav"]));
        if (path is null) return;

        IsWorking = true;
        try
        {
            var result = await _mastering.ProcessAndExportAsync(ActiveTrack.FilePath, path, BuildMasteringSettings());
            MasteringStatus = $"Mastered to {Path.GetFileName(path)} · {result.InputLufs:0.0} → {result.OutputLufs:0.0} LUFS.";
            await AddAudioAsync(path);
        }
        catch (Exception ex) { MasteringStatus = $"Master export failed: {ex.Message}"; }
        finally { IsWorking = false; }
    }

    [RelayCommand]
    private void ResetMastering()
    {
        ApplyMasteringSettings(new MasteringSettings());
        MasteringStatus = "Mastering settings reset.";
    }

    [RelayCommand]
    private async Task ExportBinauralAsync()
    {
        var sources = BuildSpatialSources();
        if (sources.Count == 0)
        {
            SpatialStatus = "No readable audio tracks are available for spatial export.";
            return;
        }
        var path = await _fileDialogs.SaveFileAsync(new FileDialogOptions(
            "Export binaural 3D mix", SuggestedFileName: "audiola-binaural.wav", Extensions: ["wav"]));
        if (path is null) return;

        IsWorking = true;
        try
        {
            await Task.Run(() =>
            {
                var output = SpatialAudioService.RenderBinaural(sources);
                SpatialAudioService.WriteWav(path, output.Interleaved, 2, output.SampleRate);
            });
            SpatialStatus = $"Binaural WAV exported: {Path.GetFileName(path)}";
        }
        catch (Exception ex) { SpatialStatus = $"Binaural export failed: {ex.Message}"; }
        finally { IsWorking = false; }
    }

    [RelayCommand]
    private async Task ExportSurroundAsync()
    {
        var sources = BuildSpatialSources();
        if (sources.Count == 0)
        {
            SpatialStatus = "No readable audio tracks are available for spatial export.";
            return;
        }
        var layout = SelectedSpatialLayout;
        var path = await _fileDialogs.SaveFileAsync(new FileDialogOptions(
            "Export surround WAV", SuggestedFileName: "audiola-surround.wav", Extensions: ["wav"]));
        if (path is null) return;

        IsWorking = true;
        try
        {
            await Task.Run(() =>
            {
                var output = SpatialAudioService.RenderMultichannel(sources, layout);
                SpatialAudioService.WriteSurroundWav(path, output.Interleaved, output.Channels, output.SampleRate,
                    SpatialAudioService.ChannelMask(layout));
            });
            SpatialStatus = $"{SpatialAudioService.ChannelLabel(layout)} exported: {Path.GetFileName(path)}";
        }
        catch (Exception ex) { SpatialStatus = $"Surround export failed: {ex.Message}"; }
        finally { IsWorking = false; }
    }

    [RelayCommand]
    private void AutoArrangeSpatial()
    {
        if (SpatialSources.Count == 0) EnsureSpatialSources();
        var count = SpatialSources.Count;
        for (var i = 0; i < count; i++)
            SpatialSources[i].AzimuthDeg = count == 1 ? 0 : -70 + 140d * i / (count - 1);
        SpatialStatus = "Sources arranged across the front stage.";
    }

    [RelayCommand]
    private async Task LoadVoiceModelsAsync()
    {
        IsWorking = true;
        try
        {
            var models = await _voices.GetModelsAsync();
            VoiceModels.Clear();
            foreach (var model in models) VoiceModels.Add(model);
            VoiceStatus = VoiceEngineAvailable
                ? $"{VoiceModels.Count} local voice models found."
                : VoiceEngineMessage;
        }
        catch (Exception ex) { VoiceStatus = $"Voice model query failed: {ex.Message}"; }
        finally { IsWorking = false; }
    }

    [RelayCommand]
    private async Task AddVoiceProfileAsync()
    {
        var sample = (await _fileDialogs.OpenFilesAsync(new FileDialogOptions(
            "Choose an owned or licensed voice sample", Extensions: AudioExtensions))).FirstOrDefault();
        if (sample is null) return;
        var model = VoiceModels.FirstOrDefault();
        var name = Path.GetFileNameWithoutExtension(sample);
        _voiceProfiles.Add(new VoiceProfile
        {
            Name = name,
            Description = "Reference sample stored locally; use only a voice you own or are licensed to use.",
            ModelId = model?.Id ?? "",
            Language = "en"
        }, [sample]);
        RefreshVoiceProfiles();
        VoiceStatus = $"Voice profile “{name}” stored locally.";
    }

    [RelayCommand]
    private async Task DownloadVoiceModelAsync(LocalVoiceModel? model)
    {
        if (model is null) return;
        if (!VoiceEngineAvailable)
        {
            VoiceStatus = VoiceEngineMessage;
            return;
        }

        IsWorking = true;
        try
        {
            await _voices.DownloadModelAsync(model.Id, new Progress<string>(s => VoiceStatus = s));
            VoiceStatus = $"{model.Name} installed.";
            await LoadVoiceModelsAsync();
        }
        catch (Exception ex) { VoiceStatus = $"Model installation failed: {ex.Message}"; }
        finally { IsWorking = false; }
    }

    [RelayCommand(CanExecute = nameof(CanSynthesizeVoice))]
    private async Task SynthesizeVoiceAsync()
    {
        if (SelectedVoiceProfile is null) return;
        if (!VoiceEngineAvailable)
        {
            VoiceStatus = VoiceEngineMessage;
            return;
        }

        var path = await _fileDialogs.SaveFileAsync(new FileDialogOptions(
            "Export generated voice", SuggestedFileName: $"{SanitizeFileName(SelectedVoiceProfile.Name)}-tts.wav", Extensions: ["wav"]));
        if (path is null) return;

        IsWorking = true;
        try
        {
            var result = await _voices.SpeakAsync(VoiceText, SelectedVoiceProfile, VoiceSpeed);
            await Task.Run(() => AudioExporter.Export(result.Samples, result.SampleRate, 2, path));
            await AddAudioAsync(path, SelectedVoiceProfile.Name);
            VoiceStatus = $"Generated voice exported: {Path.GetFileName(path)}";
        }
        catch (Exception ex) { VoiceStatus = $"Voice generation failed: {ex.Message}"; }
        finally { IsWorking = false; }
    }

    [RelayCommand]
    private void RemoveVoiceProfile(VoiceProfile? profile)
    {
        if (profile is null) return;
        _voiceProfiles.Remove(profile.Id);
        RefreshVoiceProfiles();
        VoiceStatus = $"Voice profile “{profile.Name}” removed.";
    }

    [RelayCommand(CanExecute = nameof(HasAudio))]
    private async Task AnalyzeCurrentTrackAsync()
    {
        if (ActiveTrack is null) return;
        await AnalyzeProvenanceAsync(ActiveTrack.FilePath);
    }

    [RelayCommand]
    private async Task AnalyzeFileAsync()
    {
        var path = (await _fileDialogs.OpenFilesAsync(new FileDialogOptions(
            "Choose audio for provenance analysis", Extensions: AudioExtensions))).FirstOrDefault();
        if (path is not null) await AnalyzeProvenanceAsync(path);
    }

    [RelayCommand]
    private async Task LoadEvaluationCsvAsync()
    {
        var path = (await _fileDialogs.OpenFilesAsync(new FileDialogOptions(
            "Open evaluation CSV", Extensions: ["csv", "tsv", "txt"]))).FirstOrDefault();
        if (path is null) return;
        try
        {
            _evaluationRows = await _evaluation.ParseAsync(path);
            EvaluationHasScores = _evaluation.HasScores(_evaluationRows);
            RecomputeEvaluation();
            EvaluationStatus = $"{_evaluationRows.Count} data row(s) loaded from {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            EvaluationHasResult = false;
            EvaluationStatus = $"CSV load failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ChooseCoverAsync()
    {
        var path = (await _fileDialogs.OpenFilesAsync(new FileDialogOptions(
            "Choose cover artwork", Extensions: ["jpg", "jpeg", "png", "webp", "bmp"]))).FirstOrDefault();
        if (path is null) return;
        try
        {
            _metadata.CoverArt = await File.ReadAllBytesAsync(path);
            _metadata.CoverMimeType = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".png" => "image/png", ".webp" => "image/webp", ".bmp" => "image/bmp", _ => "image/jpeg"
            };
            MetadataCoverStatus = $"Cover: {Path.GetFileName(path)}";
        }
        catch (Exception ex) { MetadataCoverStatus = $"Could not read cover: {ex.Message}"; }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _settings.Current.PythonPath = PythonPath;
        _settings.Current.DemucsModel = DemucsModel;
        _settings.Current.OutputDirectory = StemOutputDirectory;
        _settings.Save();
        SettingsStatus = "Settings saved.";
    }

    [RelayCommand]
    private async Task CheckDemucsAsync()
    {
        SaveSettings();
        SettingsStatus = await _stemSeparator.IsAvailableAsync()
            ? "Demucs is available in the configured Python environment."
            : "Demucs is unavailable. Install it with: pip install -U demucs";
    }

    [RelayCommand]
    private void SetTheme(string? theme)
    {
        var normalized = theme is "Dark" or "Light" ? theme : "System";
        _settings.Current.Theme = normalized;
        _settings.Save();
        Application.Current!.RequestedThemeVariant = normalized switch
        {
            "Dark" => ThemeVariant.Dark,
            "Light" => ThemeVariant.Light,
            _ => ThemeVariant.Default
        };
        SettingsStatus = $"Theme set to {normalized}.";
    }

    private async Task AddAudioAsync(string path, string? name = null)
    {
        var track = await _waveforms.LoadAsync(path);
        var dto = new ProjectTrackDto
        {
            Name = name ?? Path.GetFileNameWithoutExtension(path),
            Clips =
            [
                new ProjectClipDto
                {
                    Media = path,
                    SourceTotalSeconds = track.Duration.TotalSeconds,
                    LengthSeconds = track.Duration.TotalSeconds
                }
            ]
        };
        var item = new ProjectTrackItem(dto, track);
        Tracks.Add(item);
        SelectedTrack = item;
        AddRecent(_settings.Current.RecentFiles, path);
        _settings.Save();
        RefreshRecents();
        EnsureSpatialSources();
        SaveProjectCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadProjectAsync(ProjectDto project)
    {
        Tracks.Clear();
        foreach (var dto in project.Tracks)
        {
            AudioTrack? track = null;
            var source = dto.Clips.FirstOrDefault()?.Media;
            if (!string.IsNullOrWhiteSpace(source) && File.Exists(source))
            {
                try { track = await _waveforms.LoadAsync(source); }
                catch { }
            }
            Tracks.Add(new ProjectTrackItem(dto, track));
        }
        ImportMetadata(project.Metadata);
        ApplyMasteringSettings(project.Mastering ?? new MasteringSettings());
        SelectedTrack = Tracks.ElementAtOrDefault(project.SelectedTrackIndex >= 0 ? project.SelectedTrackIndex : 0);
        EnsureSpatialSources(project.Spatial);
        OnPropertyChanged(nameof(HasTracks));
        SaveProjectCommand.NotifyCanExecuteChanged();
    }

    private ProjectDto BuildProject() => new()
    {
        Tracks = Tracks.Select(t => t.ToDto()).ToList(),
        SelectedTrackIndex = SelectedTrack is null ? -1 : Tracks.IndexOf(SelectedTrack),
        Mastering = BuildMasteringSettings(),
        MasteringProfile = SelectedMasteringProfile,
        Metadata = BuildMetadata(),
        Spatial = new ProjectSpatialDto
        {
            Layout = SelectedSpatialLayout.ToString(),
            Sources = SpatialSources.Select(s => new ProjectSpatialSourceDto
            {
                Name = s.Name,
                AzimuthDeg = s.AzimuthDeg,
                ElevationDeg = s.ElevationDeg,
                Distance = s.Distance,
                GainDb = s.GainDb,
                Muted = s.Muted
            }).ToList()
        }
    };

    private MasteringSettings BuildMasteringSettings() => new()
    {
        HighPassEnabled = HighPassEnabled,
        HighPassHz = HighPassHz,
        CompressorEnabled = CompressorEnabled,
        ThresholdDb = ThresholdDb,
        Ratio = Ratio,
        LowShelfGainDb = LowShelfGainDb,
        MidGainDb = MidGainDb,
        HighShelfGainDb = HighShelfGainDb,
        NormalizeLoudness = NormalizeLoudness,
        TargetLufs = TargetLufs
    };

    private void ApplyMasteringSettings(MasteringSettings settings)
    {
        HighPassEnabled = settings.HighPassEnabled;
        HighPassHz = settings.HighPassHz;
        CompressorEnabled = settings.CompressorEnabled;
        ThresholdDb = settings.ThresholdDb;
        Ratio = settings.Ratio;
        LowShelfGainDb = settings.LowShelfGainDb;
        MidGainDb = settings.MidGainDb;
        HighShelfGainDb = settings.HighShelfGainDb;
        NormalizeLoudness = settings.NormalizeLoudness;
        TargetLufs = settings.TargetLufs;
    }

    private static IReadOnlyList<MasteringProfile> MasteringProfilesCore() => Audiola.Models.MasteringProfiles.All;

    private void EnsureSpatialSources(ProjectSpatialDto? saved = null)
    {
        var existing = SpatialSources.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        var savedSources = saved?.Sources.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, ProjectSpatialSourceDto>(StringComparer.OrdinalIgnoreCase);
        SpatialSources.Clear();
        var usable = Tracks.Where(t => t.SourcePath is not null).ToList();
        for (var i = 0; i < usable.Count; i++)
        {
            var track = usable[i];
            var source = new SpatialSourceItem(track.Name, track.SourcePath!);
            if (savedSources.TryGetValue(track.Name, out var fromProject))
                source.Apply(fromProject);
            else if (existing.TryGetValue(track.Name, out var previous))
                source.CopyFrom(previous);
            else
                source.AzimuthDeg = usable.Count == 1 ? 0 : -70 + i * 140d / (usable.Count - 1);
            SpatialSources.Add(source);
        }
    }

    private List<SpatialSource> BuildSpatialSources() => SpatialSources
        .Where(s => File.Exists(s.FilePath))
        .Select(s => new SpatialSource(s.FilePath, s.AzimuthDeg, s.ElevationDeg, s.Distance, s.GainDb, s.Muted))
        .ToList();

    private SpatialLayout SelectedSpatialLayout => SelectedSpatialLayoutName switch
    {
        "5.1" => SpatialLayout.Surround51,
        "7.1" => SpatialLayout.Surround71,
        _ => SpatialLayout.Atmos714
    };

    [ObservableProperty] private string _selectedSpatialLayoutName = "7.1.4";
    public IReadOnlyList<string> SpatialLayouts { get; } = ["5.1", "7.1", "7.1.4"];

    private async Task AnalyzeProvenanceAsync(string path)
    {
        IsWorking = true;
        try
        {
            var report = await _provenance.AnalyzeAsync(path);
            ProvenanceFindings.Clear();
            foreach (var finding in report.Findings) ProvenanceFindings.Add(finding);
            ProvenanceAssessment = report.Assessment;
            ProvenanceRaw = report.C2paRaw ?? "";
            ProvenanceStatus = report.C2paToolAvailable
                ? $"{report.Findings.Count} finding(s), including c2patool output when available."
                : $"{report.Findings.Count} finding(s). {C2paHelp}";
        }
        catch (Exception ex) { ProvenanceStatus = $"Provenance analysis failed: {ex.Message}"; }
        finally { IsWorking = false; }
    }

    private void EnsurePlaybackOpen()
    {
        if (ActiveTrack is null)
            throw new InvalidOperationException("Select an audio track before controlling playback.");

        if (string.Equals(_openedPlaybackPath, ActiveTrack.FilePath, StringComparison.OrdinalIgnoreCase))
            return;

        _playback.Stop();
        _playback.Open(ActiveTrack.FilePath);
        _openedPlaybackPath = ActiveTrack.FilePath;
        PlaybackDurationText = FormatTime(_playback.Duration);
        SetPlaybackProgress(0);
        PlaybackPositionText = "00:00";
    }

    private void StopPlayback()
    {
        _playbackTimer.Stop();
        if (_openedPlaybackPath is not null)
            _playback.Stop();
        _openedPlaybackPath = null;
        IsPlaying = false;
        PlaybackPositionText = "00:00";
        PlaybackDurationText = FormatTime(ActiveTrack?.Duration ?? TimeSpan.Zero);
        SetPlaybackProgress(0);
    }

    private void OnPlaybackTimerTick(object? sender, EventArgs e)
    {
        UpdatePlaybackState();
        if (!_playback.IsPlaying)
        {
            IsPlaying = false;
            _playbackTimer.Stop();
        }
    }

    private void OnPlaybackEnded(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() =>
    {
        UpdatePlaybackState();
        IsPlaying = false;
        _playbackTimer.Stop();
        Status = "Playback finished.";
    });

    private void UpdatePlaybackState()
    {
        if (_openedPlaybackPath is null) return;
        var duration = _playback.Duration;
        var position = _playback.Position;
        PlaybackDurationText = FormatTime(duration);
        PlaybackPositionText = FormatTime(position);
        SetPlaybackProgress(duration > TimeSpan.Zero ? position.TotalSeconds / duration.TotalSeconds : 0);
    }

    private void SetPlaybackProgress(double value)
    {
        _updatingPlaybackProgress = true;
        PlaybackProgress = Math.Clamp(value, 0, 1);
        _updatingPlaybackProgress = false;
    }

    private static string FormatTime(TimeSpan value) => $"{(int)value.TotalMinutes:00}:{value.Seconds:00}";

    private void RecomputeEvaluation()
    {
        var result = _evaluation.Evaluate(_evaluationRows, EvaluationThreshold);
        TruePositives = result.TruePositives;
        FalsePositives = result.FalsePositives;
        TrueNegatives = result.TrueNegatives;
        FalseNegatives = result.FalseNegatives;
        EvaluationSkipped = result.Skipped;
        Accuracy = Percent(result.Accuracy);
        Precision = Percent(result.Precision);
        Recall = Percent(result.Recall);
        F1 = double.IsNaN(result.F1) ? "—" : result.F1.ToString("0.000");
        Specificity = Percent(result.Specificity);
        FalsePositiveRate = Percent(result.FalsePositiveRate);
        FalseNegativeRate = Percent(result.FalseNegativeRate);
        EvaluationHasResult = true;
    }

    private void RefreshRecents()
    {
        RecentFiles.Clear();
        foreach (var path in _settings.Current.RecentFiles.Where(File.Exists))
            RecentFiles.Add(new RecentItem(path, false));
        RecentProjects.Clear();
        foreach (var path in _settings.Current.RecentProjects.Where(File.Exists))
            RecentProjects.Add(new RecentItem(path, true));
    }

    private void RefreshVoiceProfiles()
    {
        VoiceProfiles.Clear();
        foreach (var profile in _voiceProfiles.Profiles) VoiceProfiles.Add(profile);
        SelectedVoiceProfile = VoiceProfiles.FirstOrDefault();
    }

    private static void AddRecent(List<string> values, string path)
    {
        values.RemoveAll(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase));
        values.Insert(0, path);
        if (values.Count > 12) values.RemoveRange(12, values.Count - 12);
    }

    private AudioMetadata BuildMetadata()
    {
        ApplyMetadata();
        return _metadata.Clone();
    }

    private void ApplyMetadata()
    {
        _metadata.Title = MetadataTitle;
        _metadata.Artist = MetadataArtist;
        _metadata.Album = MetadataAlbum;
        _metadata.Genre = MetadataGenre;
        _metadata.Lyrics = MetadataLyrics;
    }

    private void ImportMetadata(AudioMetadata? metadata)
    {
        if (metadata is null) return;
        _metadata.Title = metadata.Title;
        _metadata.Artist = metadata.Artist;
        _metadata.Album = metadata.Album;
        _metadata.AlbumArtist = metadata.AlbumArtist;
        _metadata.Genre = metadata.Genre;
        _metadata.Year = metadata.Year;
        _metadata.TrackNumber = metadata.TrackNumber;
        _metadata.Comment = metadata.Comment;
        _metadata.Lyrics = metadata.Lyrics;
        _metadata.CoverArt = metadata.CoverArt;
        _metadata.CoverMimeType = metadata.CoverMimeType;
        MetadataTitle = metadata.Title ?? "";
        MetadataArtist = metadata.Artist ?? "";
        MetadataAlbum = metadata.Album ?? "";
        MetadataGenre = metadata.Genre ?? "";
        MetadataLyrics = metadata.Lyrics ?? "";
        MetadataCoverStatus = metadata.HasCover ? "Cover artwork stored in project" : "No cover artwork selected";
    }

    partial void OnPythonPathChanged(string value) => _settings.Current.PythonPath = value;
    partial void OnDemucsModelChanged(string value) => _settings.Current.DemucsModel = value;
    partial void OnStemOutputDirectoryChanged(string value) => _settings.Current.OutputDirectory = value;

    private static string Percent(double value) => double.IsNaN(value) ? "—" : $"{value * 100:0.0}%";
    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(c => invalid.Contains(c) ? '_' : c));
    }

    public void Dispose()
    {
        _playbackTimer.Stop();
        _playbackTimer.Tick -= OnPlaybackTimerTick;
        _playback.PlaybackEnded -= OnPlaybackEnded;
        _playback.Dispose();
    }
}

public sealed partial class NavigationItem : ObservableObject
{
    public NavigationItem(string title, string icon, WorkspacePageViewModel content)
    {
        Title = title;
        Icon = icon;
        Content = content;
    }

    public string Title { get; }
    public string Icon { get; }
    public WorkspacePageViewModel Content { get; }
    [ObservableProperty] private bool _isSelected;
}

public sealed partial class ProjectTrackItem : ObservableObject
{
    private readonly ProjectTrackDto _model;

    public ProjectTrackItem(ProjectTrackDto model, AudioTrack? waveform)
    {
        _model = model;
        Waveform = waveform;
        _name = string.IsNullOrWhiteSpace(model.Name) ? waveform?.FileName ?? "Missing media" : model.Name;
        _volume = model.Volume;
        _pan = model.Pan;
        _isMuted = model.IsMuted;
    }

    public AudioTrack? Waveform { get; }
    public string? SourcePath => _model.Clips.FirstOrDefault()?.Media;
    public string DurationText => Waveform is null ? "Media unavailable" : $"{Waveform.Duration:mm\\:ss} · {Waveform.SampleRate:N0} Hz · {Waveform.Channels} ch";
    [ObservableProperty] private string _name;
    [ObservableProperty] private double _volume;
    [ObservableProperty] private double _pan;
    [ObservableProperty] private bool _isMuted;

    public ProjectTrackDto ToDto()
    {
        _model.Name = Name;
        _model.Volume = Volume;
        _model.Pan = Pan;
        _model.IsMuted = IsMuted;
        return _model;
    }
}

public sealed partial class SpatialSourceItem : ObservableObject
{
    public SpatialSourceItem(string name, string filePath)
    {
        Name = name;
        FilePath = filePath;
    }

    public string Name { get; }
    public string FilePath { get; }
    [ObservableProperty] private double _azimuthDeg;
    [ObservableProperty] private double _elevationDeg;
    [ObservableProperty] private double _distance = 1;
    [ObservableProperty] private double _gainDb;
    [ObservableProperty] private bool _muted;

    public void Apply(ProjectSpatialSourceDto source)
    {
        AzimuthDeg = source.AzimuthDeg;
        ElevationDeg = source.ElevationDeg;
        Distance = source.Distance;
        GainDb = source.GainDb;
        Muted = source.Muted;
    }

    public void CopyFrom(SpatialSourceItem source)
    {
        AzimuthDeg = source.AzimuthDeg;
        ElevationDeg = source.ElevationDeg;
        Distance = source.Distance;
        GainDb = source.GainDb;
        Muted = source.Muted;
    }
}

public sealed class RecentItem
{
    public RecentItem(string path, bool isProject)
    {
        Path = path;
        IsProject = isProject;
        Name = isProject ? System.IO.Path.GetFileNameWithoutExtension(path) : System.IO.Path.GetFileName(path);
        try
        {
            var file = new FileInfo(path);
            Details = $"{file.Length / 1024d / 1024d:0.0} MB · {file.LastWriteTime:g}";
        }
        catch { Details = path; }
    }

    public string Path { get; }
    public string Name { get; }
    public bool IsProject { get; }
    public string Details { get; }
}
