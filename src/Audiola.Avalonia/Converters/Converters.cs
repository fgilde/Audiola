using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Audiola.Avalonia.Converters;

/// <summary>
/// Wandelt einen Hex-Farbstring ("#RRGGBB") in einen <see cref="SolidColorBrush"/> —
/// Gegenstück zum gleichnamigen WPF-Konverter (die ViewModels liefern Farben als String,
/// weil WPF und Avalonia unterschiedliche Brush-Typen haben).
/// </summary>
public sealed class StringToBrushConverter : IValueConverter
{
    public static readonly StringToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            try { return new SolidColorBrush(Color.Parse(s)); }
            catch { /* ungültiger String → Fallback unten */ }
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// True, wenn der Wert dem als Parameter übergebenen Enum-Namen entspricht. Ersetzt WPFs
/// <c>DataTrigger</c> in DataTemplates, das Avalonia nicht kennt.
/// </summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null && parameter is string name
           && string.Equals(value.ToString(), name, StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Cover-Bytes (aus den Tags) als anzeigbares Bitmap — Gegenstück zum WPF-BytesToImageConverter.
/// </summary>
public sealed class BytesToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] { Length: > 0 } bytes) return null;
        try
        {
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        catch { return null; }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>[Sekunden, Pixel/Sekunde] → Breite in Pixeln (für Clips und Auswahlbereiche).</summary>
public sealed class SecondsToWidthConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        => values.Count >= 2 && values[0] is double seconds && values[1] is double pixelsPerSecond
            ? Math.Max(2, seconds * pixelsPerSecond)
            : 0d;
}

/// <summary>[Sekunden, Pixel/Sekunde] → linker Rand für die Position auf der Zeitachse.</summary>
public sealed class SecondsToMarginConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        => values.Count >= 2 && values[0] is double seconds && values[1] is double pixelsPerSecond
            ? new Thickness(Math.Max(0, seconds * pixelsPerSecond), 0, 0, 0)
            : new Thickness(0);
}

/// <summary>Pixel-X → linker Rand (Thickness) — für den Playhead, dessen VM nur die X-Position kennt.</summary>
public sealed class PixelsToLeftMarginConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => new Thickness(value is double x ? Math.Max(0, x) : 0, 0, 0, 0);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
