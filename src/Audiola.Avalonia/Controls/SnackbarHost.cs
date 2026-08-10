using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Animation;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FluentIcons.Avalonia;
using FluentIcons.Common;

namespace Audiola.Controls;

/// <summary>Aussehen einer Snackbar-Meldung — entspricht den vier Stufen der WPF-Fassung.</summary>
public enum SnackbarKind
{
    Success,
    Info,
    Warning,
    Error
}

/// <summary>
/// Toast-Bereich am unteren Rand: zeigt kurzlebige Meldungen übereinander an.
/// Ersatz für WPF-UIs <c>SnackbarPresenter</c>, den Avalonia nicht hat.
/// </summary>
public sealed class SnackbarHost : TemplatedControl
{
    private readonly StackPanel _stack = new()
    {
        Orientation = Orientation.Vertical,
        HorizontalAlignment = HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Bottom,
        Margin = new Thickness(16, 16, 16, 24),
        Spacing = 8
    };

    public SnackbarHost()
    {
        IsHitTestVisible = false;                 // Toasts sollen Klicks nicht blocken
        Template = new FuncControlTemplate((_, _) => _stack);
    }

    /// <summary>Zeigt eine Meldung und blendet sie nach <paramref name="seconds"/> wieder aus.</summary>
    public void Show(SnackbarKind kind, string title, string message, int seconds)
    {
        var (accent, glyph) = kind switch
        {
            SnackbarKind.Success => ("#3DDC84", Symbol.CheckmarkCircle),
            SnackbarKind.Warning => ("#FFC24B", Symbol.Warning),
            SnackbarKind.Error => ("#FF5350", Symbol.ErrorCircle),
            _ => ("#3F8CFF", Symbol.Info)
        };

        var card = BuildCard(Color.Parse(accent), glyph, title, message);
        _stack.Children.Add(card);

        DispatcherTimer.RunOnce(() =>
        {
            // Sanft ausblenden, dann entfernen (kein Springen der übrigen Toasts).
            card.Opacity = 0;
            DispatcherTimer.RunOnce(() => _stack.Children.Remove(card), TimeSpan.FromMilliseconds(220));
        }, TimeSpan.FromSeconds(Math.Max(1, seconds)));
    }

    private static Border BuildCard(Color accent, Symbol glyph, string title, string message)
    {
        var text = new StackPanel { Spacing = 2 };
        text.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Colors.White)
        });
        if (!string.IsNullOrWhiteSpace(message))
            text.Children.Add(new TextBlock
            {
                Text = message,
                MaxWidth = 380,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(0xC9, 0xCF, 0xDE))
            });

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        row.Children.Add(new SymbolIcon
        {
            Symbol = glyph,
            FontSize = 20,
            Foreground = new SolidColorBrush(accent),
            VerticalAlignment = VerticalAlignment.Top
        });
        row.Children.Add(text);

        return new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x20, 0x23, 0x2D)),
            BorderBrush = new SolidColorBrush(accent),
            BorderThickness = new Thickness(1, 1, 1, 1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10),
            Child = row,
            Transitions =
            [
                new DoubleTransition
                {
                    Property = OpacityProperty,
                    Duration = TimeSpan.FromMilliseconds(200),
                    Easing = new CubicEaseOut()
                }
            ]
        };
    }
}
