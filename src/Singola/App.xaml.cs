using System.IO;
using System.Windows;
using Wpf.Ui.Appearance;

namespace Singola;

/// <summary>
/// Singola — die Karaoke-Bühne der Audiola-Familie. Immer dunkel (Party-Look);
/// Einstellungen (ElevenLabs-Key) werden mit Audiola geteilt (gleiche settings.json).
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
            ApplicationAccentColorManager.Apply(
                System.Windows.Media.Color.FromRgb(0xB5, 0x6B, 0xFF), ApplicationTheme.Dark, systemGlassColor: false);

            var window = new MainWindow();
            // Per Doppelklick/Kommandozeile übergebener Song.
            window.PendingStartupFile = e.Args.FirstOrDefault(File.Exists);
            window.Show();
        }
        catch (Exception ex)
        {
            try
            {
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "singola.log"),
                    $"[{DateTimeOffset.UtcNow:O}] [OnStartup] {ex}\n\n");
            }
            catch { }
            throw;
        }
    }
}
