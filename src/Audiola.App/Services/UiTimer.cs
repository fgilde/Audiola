namespace Audiola.Services;

/// <summary>
/// Wiederkehrender Tick auf dem UI-Thread — plattformneutraler Ersatz für WPFs
/// <c>DispatcherTimer</c> (gleiche Member, damit Aufrufer nur den Typnamen tauschen).
/// Ticks überlappen nicht: solange ein Tick noch in der UI-Warteschlange hängt, wird
/// kein weiterer eingereiht (so verhält sich auch der DispatcherTimer).
/// </summary>
public sealed class UiTimer : IDisposable
{
    private readonly System.Timers.Timer _timer = new() { AutoReset = true };
    private int _pending;

    public UiTimer()
    {
        _timer.Elapsed += (_, _) =>
        {
            if (Interlocked.Exchange(ref _pending, 1) == 1) return;   // vorheriger Tick läuft noch
            DispatcherHelper.PostToUi(() =>
            {
                Interlocked.Exchange(ref _pending, 0);
                Tick?.Invoke(this, EventArgs.Empty);
            });
        };
    }

    public event EventHandler? Tick;

    public TimeSpan Interval
    {
        get => TimeSpan.FromMilliseconds(_timer.Interval);
        set => _timer.Interval = Math.Max(1, value.TotalMilliseconds);
    }

    public bool IsEnabled
    {
        get => _timer.Enabled;
        set => _timer.Enabled = value;
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public void Dispose() => _timer.Dispose();
}
