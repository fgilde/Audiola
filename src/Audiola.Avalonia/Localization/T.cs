using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Audiola.Avalonia.Localization;

/// <summary>XAML: <c>Text="{l:T 'Deutscher Text'}"</c> — übersetzt beim Laden.</summary>
public sealed class T(string text) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => L.T(text);
}

/// <summary>
/// Übersetzte StringFormat-Alternative für Bindings (ohne Parameter: übersetzt den Wert selbst):
/// <c>{Binding X, Converter={x:Static l:Fmt.Instance}, ConverterParameter='Ziel: {0:F0} LUFS'}</c>.
/// </summary>
public sealed class Fmt : IValueConverter
{
    public static Fmt Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => parameter switch
    {
        // "{}" ist das XAML-Escape am Anfang — je nach Parser kommt es mit an.
        string format => string.Format(culture, L.T(format.StartsWith("{}") ? format[2..] : format), value),
        // Ohne Vorlage: den gebundenen Text selbst übersetzen (z. B. Profilnamen, die als Schlüssel dienen).
        _ => value is string text ? L.T(text) : value,
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
