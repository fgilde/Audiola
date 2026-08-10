using Audiola.Services;
using Avalonia;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Audiola.Avalonia.Platform;

/// <summary>
/// Eigene Fenster der Avalonia-Fassung. Rückfragen sind vollständig; die großen Dialoge
/// (Export, Spur mastern, Einsing-Studio, Vorschau, Einrichtungs-Assistent) werden in der
/// laufenden Migration nachgezogen und melden sich bis dahin als noch nicht verfügbar.
/// </summary>
public sealed class AvaloniaAppDialogs(INotifier notifier) : IAppDialogs
{
    public bool Confirm(string title, string message)
    {
        // Synchron aus ViewModels aufgerufen; Avalonia-Dialoge sind async — daher pumpen wir
        // die Antwort über einen verschachtelten Dispatcher-Frame zurück.
        var task = AskAsync(title, message, ("Ja", true), ("Nein", false));
        return WaitOnUiThread(task);
    }

    public Task<SaveDiscardCancel> AskSaveDiscardCancelAsync(string title, string message)
        => AskAsync(title, message,
            ("Speichern", SaveDiscardCancel.Save),
            ("Verwerfen", SaveDiscardCancel.Discard),
            ("Abbrechen", SaveDiscardCancel.Cancel));

    public void ShowTrackMastering(object trackViewModel) => NotPortedYet("Spur mastern");

    public void OpenSingAlong() => NotPortedYet("Einsing-Studio");

    public void ShowSetupWizard() => NotPortedYet("Einrichtungs-Assistent");

    public Task<ExportRequest?> ShowExportAsync(ExportDialogRequest request)
    {
        NotPortedYet("Export-Dialog");
        return Task.FromResult<ExportRequest?>(null);
    }

    public Task ShowFilePreviewAsync(string url, string fileName)
    {
        // Kein eingebetteter Browser: im Systembrowser öffnen (funktioniert auf allen Plattformen).
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            notifier.Error("Vorschau fehlgeschlagen", ex.Message);
        }
        return Task.CompletedTask;
    }

    public Task<VariationChoice?> PickVariationsAsync(IReadOnlyList<IAudioVariationProvider> providers, string scope)
    {
        NotPortedYet("Variationen-Auswahl");
        return Task.FromResult<VariationChoice?>(null);
    }

    public Task<ViewModels.VoiceChoice?> PickVoiceAsync()
    {
        NotPortedYet("Stimmen-Auswahl");
        return Task.FromResult<ViewModels.VoiceChoice?>(null);
    }

    public Task<TextToSpeechRequest?> AskTextToSpeechAsync()
    {
        NotPortedYet("Text zu Sprache");
        return Task.FromResult<TextToSpeechRequest?>(null);
    }

    private void NotPortedYet(string what) =>
        notifier.Warning(what, "Wird gerade auf die plattformübergreifende Oberfläche portiert.", 5);

    /// <summary>Baut eine schlichte modale Rückfrage mit beliebigen Antwortknöpfen.</summary>
    private static Task<T> AskAsync<T>(string title, string message, params (string Label, T Result)[] choices)
    {
        var completion = new TaskCompletionSource<T>();
        var dialog = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.Parse("#1A1C23"))
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 18, 0, 0)
        };
        foreach (var (label, result) in choices)
        {
            var button = new Button { Content = label, MinWidth = 92 };
            button.Click += (_, _) => { completion.TrySetResult(result); dialog.Close(); };
            buttons.Children.Add(button);
        }

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(22, 18),
            MaxWidth = 460,
            Children =
            {
                new TextBlock
                {
                    Text = title, FontSize = 17, FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.Parse("#E9EBF2"))
                },
                new TextBlock
                {
                    Text = message, Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.Parse("#A7ADC0"))
                },
                buttons
            }
        };

        // Fenster-X = letzte Wahl (üblicherweise „Abbrechen").
        dialog.Closed += (_, _) => completion.TrySetResult(choices[^1].Result);

        var owner = HostWindow.Active;
        if (owner is not null) dialog.ShowDialog(owner);
        else dialog.Show();
        return completion.Task;
    }

    /// <summary>Wartet auf dem UI-Thread, ohne ihn zu blockieren (verschachtelte Nachrichtenschleife).</summary>
    private static T WaitOnUiThread<T>(Task<T> task)
    {
        if (!task.IsCompleted)
        {
            var frame = new DispatcherFrame();
            task.ContinueWith(_ => frame.Continue = false,
                TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.UIThread.PushFrame(frame);
        }
        return task.GetAwaiter().GetResult();
    }
}
