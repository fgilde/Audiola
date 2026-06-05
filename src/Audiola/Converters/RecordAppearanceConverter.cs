using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace Audiola.Converters;

/// <summary>true (Aufnahme läuft) -&gt; Danger (rot), sonst Secondary.</summary>
public sealed class RecordAppearanceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? ControlAppearance.Danger : ControlAppearance.Secondary;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
