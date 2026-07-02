using System.Windows.Threading;

namespace Audiola.Services;

/// <summary>
/// UI-Thread-Marshalling in einer Zeile — ersetzt das mehrfach duplizierte
/// CheckAccess/BeginInvoke-Muster (Audio-Callbacks kommen von WaveIn/WaveOut-Threads).
/// </summary>
public static class DispatcherHelper
{
    private static Dispatcher? Ui => System.Windows.Application.Current?.Dispatcher;

    /// <summary>Führt die Aktion synchron auf dem UI-Thread aus (direkt, wenn schon dort).</summary>
    public static void OnUi(Action action)
    {
        var d = Ui;
        if (d is null || d.CheckAccess()) action();
        else d.Invoke(action);
    }

    /// <summary>Reiht die Aktion auf dem UI-Thread ein, ohne zu blockieren (direkt, wenn schon dort).</summary>
    public static void PostToUi(Action action)
    {
        var d = Ui;
        if (d is null || d.CheckAccess()) action();
        else d.BeginInvoke(action);
    }
}
