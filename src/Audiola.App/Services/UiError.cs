namespace Audiola.Services;

/// <summary>
/// Zeigt Fehler in einer kopierbaren MessageBox (Strg+C kopiert den ganzen Text) —
/// im Gegensatz zum flüchtigen Snackbar-Toast, den man nicht markieren kann.
/// Der Host verdrahtet seine Fenster-Implementierung über <see cref="Configure"/>.
/// </summary>
public static class UiError
{
    private static Action<string, string>? _show;

    /// <summary>Vom Host einmalig beim Start gesetzt.</summary>
    public static void Configure(Action<string, string> show) => _show = show;

    public static void Show(string title, string message)
        => DispatcherHelper.OnUi(() => _show?.Invoke(title, message));
}
