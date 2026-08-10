using System.Globalization;
using System.Windows.Data;
using Audiola.ViewModels;

namespace Audiola.Converters;

/// <summary>True für Kategorie-Kopfzeilen der Stimmenliste (die dürfen nicht wählbar sein).</summary>
public sealed class IsVoiceGroupHeaderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is VoiceGroupHeader;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
