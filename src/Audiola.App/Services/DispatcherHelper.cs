namespace Audiola.Services;

/// <summary>
/// UI-Thread-Marshalling in einer Zeile — ersetzt das mehrfach duplizierte
/// CheckAccess/BeginInvoke-Muster (Audio-Callbacks kommen von WaveIn/WaveOut-Threads).
///
/// Der Host (WPF, Avalonia) verdrahtet beim Start seinen Dispatcher über
/// <see cref="Configure"/>. Ohne Verdrahtung (Tests, Konsole) läuft alles inline.
/// </summary>
public static class DispatcherHelper
{
    private static Func<bool>? _isOnUiThread;
    private static Action<Action>? _invoke;
    private static Action<Action>? _post;

    /// <summary>Vom Host einmalig beim Start gesetzt.</summary>
    public static void Configure(Func<bool> isOnUiThread, Action<Action> invoke, Action<Action> post)
    {
        _isOnUiThread = isOnUiThread;
        _invoke = invoke;
        _post = post;
    }

    /// <summary>True, wenn der aufrufende Thread der UI-Thread ist (oder kein Host verdrahtet ist).</summary>
    public static bool IsOnUiThread => _isOnUiThread?.Invoke() ?? true;

    /// <summary>Führt die Aktion synchron auf dem UI-Thread aus (direkt, wenn schon dort).</summary>
    public static void OnUi(Action action)
    {
        if (_invoke is null || (_isOnUiThread?.Invoke() ?? true)) action();
        else _invoke(action);
    }

    /// <summary>Reiht die Aktion auf dem UI-Thread ein, ohne zu blockieren (direkt, wenn schon dort).</summary>
    public static void PostToUi(Action action)
    {
        if (_post is null || (_isOnUiThread?.Invoke() ?? true)) action();
        else _post(action);
    }
}
