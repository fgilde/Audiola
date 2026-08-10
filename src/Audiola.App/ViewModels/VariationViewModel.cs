using System.IO;
using Audiola.Models;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Audiola.ViewModels;

public sealed partial class VariationViewModel : ObservableObject
{
    private readonly IVariationService _variationService;
    private readonly INotifier _snackbar;
    private readonly IFileDialogs _files;
    private readonly Random _random = new();

    public SessionState Session { get; }

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = "Bereit. Export erzeugt eine subtile Variante des geladenen Songs.";

    [ObservableProperty] private double _overallStrength = 35;
    [ObservableProperty] private double _timbreShift = 12;
    [ObservableProperty] private double _presenceShift = 8;
    [ObservableProperty] private double _stereoWidth = 10;
    [ObservableProperty] private double _saturation = 16;
    [ObservableProperty] private double _texture = 10;
    [ObservableProperty] private bool _matchInputLoudness = true;
    [ObservableProperty] private int _variantSeed = 13579;

    public VariationViewModel(SessionState session, IVariationService variationService, INotifier snackbar,
        IFileDialogs files)
    {
        Session = session;
        _variationService = variationService;
        _snackbar = snackbar;
        _files = files;

        Session.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SessionState.CurrentTrack) or nameof(SessionState.HasTrack))
                ExportCommand.NotifyCanExecuteChanged();
        };
    }

    private VariationSettings BuildSettings() => new()
    {
        Intensity = OverallStrength / 100.0,
        TimbreShift = TimbreShift / 100.0,
        PresenceShift = PresenceShift / 100.0,
        StereoWidth = StereoWidth / 100.0,
        Saturation = Saturation / 100.0,
        Texture = Texture / 100.0,
        MatchInputLoudness = MatchInputLoudness,
        Seed = VariantSeed
    };

    private bool CanRun => Session.HasTrack && !IsBusy;

    [RelayCommand]
    private void ApplySubtlePreset()
    {
        OverallStrength = 24;
        TimbreShift = 8;
        PresenceShift = 5;
        StereoWidth = 6;
        Saturation = 10;
        Texture = 6;
        MatchInputLoudness = true;
    }

    [RelayCommand]
    private void ApplyBalancedPreset()
    {
        OverallStrength = 42;
        TimbreShift = 14;
        PresenceShift = 9;
        StereoWidth = 12;
        Saturation = 18;
        Texture = 11;
        MatchInputLoudness = true;
    }

    [RelayCommand]
    private void ApplyStrongerPreset()
    {
        OverallStrength = 62;
        TimbreShift = 22;
        PresenceShift = 15;
        StereoWidth = 18;
        Saturation = 26;
        Texture = 18;
        MatchInputLoudness = true;
    }

    [RelayCommand]
    private void RandomizeSeed()
        => VariantSeed = _random.Next(1_000, 999_999);

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task ExportAsync()
    {
        if (Session.CurrentTrack is null)
            return;

        var target = await _files.SaveFileAsync("Klangvariation exportieren",
            Path.GetFileNameWithoutExtension(Session.CurrentTrack.FileName) + "-variation.wav",
            FileFilter.Wav, FileFilter.Any);
        if (target is null)
            return;

        IsBusy = true;
        ExportCommand.NotifyCanExecuteChanged();

        try
        {
            StatusText = "Erzeuge subtile Variante …";
            var result = await _variationService.ProcessAndExportAsync(
                Session.CurrentTrack.FilePath,
                target,
                BuildSettings());

            StatusText = BuildResultText(result);

            _snackbar.Success("Variation exportiert", Path.GetFileName(target), 3);
        }
        catch (Exception ex)
        {
            StatusText = "Fehler: " + ex.Message;
            _snackbar.Error("Variation fehlgeschlagen", ex.Message, 5);
        }
        finally
        {
            IsBusy = false;
            ExportCommand.NotifyCanExecuteChanged();
        }
    }

    private static string BuildResultText(VariationResult result)
    {
        var loudnessText = double.IsInfinity(result.InputLufs) || double.IsInfinity(result.OutputLufs)
            ? "LUFS nicht sinnvoll messbar."
            : $"Lautheit: {result.InputLufs:F1} → {result.OutputLufs:F1} LUFS";

        var normalizationText = Math.Abs(result.AppliedLoudnessGainDb) > 0.05
            ? $" | Ausgleich: {result.AppliedLoudnessGainDb:+0.0;-0.0} dB"
            : string.Empty;

        var clipText = result.ClippedSamples > 0
            ? $" | Clipping-Samples: {result.ClippedSamples}"
            : string.Empty;

        var widthText = result.StereoWidthSkipped
            ? " | Stereo-Breite uebersprungen (nahezu Mono)"
            : string.Empty;

        return $"{loudnessText}{normalizationText}{clipText} | Seed: {result.Seed}{widthText}";
    }
}
