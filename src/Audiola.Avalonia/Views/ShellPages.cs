using Audiola.Avalonia.Views.Pages;
using Audiola.Services;

namespace Audiola.Avalonia.Views;

/// <summary>
/// Zuordnung des host-neutralen Seiten-Schlüssels auf das Avalonia-Steuerelement.
/// Noch nicht portierte Seiten zeigen <see cref="PendingPage"/>.
/// </summary>
public static class ShellPages
{
    public static readonly Dictionary<ShellPage, Type> ViewTypes = new()
    {
        [ShellPage.Home] = typeof(HomePage),
        [ShellPage.Editor] = typeof(EditorPage),
        [ShellPage.Timeline] = typeof(PendingPage),
        [ShellPage.Equalizer] = typeof(EqualizerPage),
        [ShellPage.Mastering] = typeof(MasteringPage),
        [ShellPage.SpatialAudio] = typeof(SpatialAudioPage),
        [ShellPage.Voices] = typeof(VoicesPage),
        [ShellPage.Variation] = typeof(VariationPage),
        [ShellPage.Provenance] = typeof(ProvenancePage),
        [ShellPage.Evaluation] = typeof(EvaluationPage),
        [ShellPage.Settings] = typeof(SettingsPage),
        [ShellPage.Metadata] = typeof(MetadataPage),
        [ShellPage.About] = typeof(AboutPage)
    };
}
