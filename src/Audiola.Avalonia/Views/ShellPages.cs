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
        [ShellPage.Editor] = typeof(PendingPage),
        [ShellPage.Timeline] = typeof(PendingPage),
        [ShellPage.Equalizer] = typeof(PendingPage),
        [ShellPage.Mastering] = typeof(PendingPage),
        [ShellPage.SpatialAudio] = typeof(PendingPage),
        [ShellPage.Voices] = typeof(PendingPage),
        [ShellPage.Variation] = typeof(PendingPage),
        [ShellPage.Provenance] = typeof(PendingPage),
        [ShellPage.Evaluation] = typeof(PendingPage),
        [ShellPage.Settings] = typeof(PendingPage),
        [ShellPage.Metadata] = typeof(PendingPage),
        [ShellPage.About] = typeof(PendingPage)
    };
}
