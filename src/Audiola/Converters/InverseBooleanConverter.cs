using System.Globalization;
using System.Windows.Data;

namespace Audiola.Converters;

/// <summary>true -&gt; false, false -&gt; true (z. B. zum Deaktivieren während eines Vorgangs).</summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not true;
}
