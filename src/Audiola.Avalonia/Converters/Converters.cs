using System.Globalization;
using System.IO;
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
