namespace Audiola;

/// <summary>
/// Host-neutraler Zugriff auf den DI-Container. Ersetzt das frühere <c>App.GetService&lt;T&gt;()</c>
/// der WPF-App an den Stellen, an denen ViewModels sich Dienste spät holen (Zyklen vermeiden).
/// Jeder Host ruft <see cref="Configure"/> einmalig beim Start.
///
/// ponytail: Service-Locator, war vorher schon einer. Konstruktor-Injection an diesen ~13 Stellen
/// nachziehen, falls die Zyklen entfallen.
/// </summary>
public static class AppServices
{
    private static IServiceProvider? _provider;

    public static void Configure(IServiceProvider provider) => _provider = provider;

    public static T Get<T>() where T : class
        => _provider?.GetService(typeof(T)) as T
           ?? throw new InvalidOperationException($"Dienst {typeof(T)} nicht registriert.");
}
