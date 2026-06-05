using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Audiola.Converters;

/// <summary>Wandelt einen Hex-Farbstring ("#RRGGBB") in einen <see cref="SolidColorBrush"/>.</summary>
public sealed class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            try
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(s));
                brush.Freeze();
                return brush;
            }
            catch
            {
                // Ungueltiger String -> Fallback unten.
            }
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
