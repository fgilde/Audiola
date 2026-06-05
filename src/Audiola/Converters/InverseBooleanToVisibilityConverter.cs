using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Audiola.Converters;

/// <summary>true -&gt; Collapsed, false -&gt; Visible.</summary>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible ? false : true;
}
