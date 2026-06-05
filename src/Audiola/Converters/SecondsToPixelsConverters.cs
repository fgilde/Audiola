using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Audiola.Converters;

/// <summary>[Sekunden, Pixel/Sekunde] → Breite in Pixeln.</summary>
public sealed class SecondsToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is double s && values[1] is double pps)
            return Math.Max(0, s * pps);
        return 0d;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>[Sekunden, Pixel/Sekunde] → linker Rand (Thickness) für die Clip-Position.</summary>
public sealed class SecondsToMarginConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is double s && values[1] is double pps)
            return new Thickness(Math.Max(0, s * pps), 0, 0, 0);
        return new Thickness(0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
