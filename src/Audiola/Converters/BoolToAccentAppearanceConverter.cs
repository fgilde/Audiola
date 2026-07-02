using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace Audiola.Converters;

/// <summary>true → Primary (aktiv/hervorgehoben), false → Secondary. Für Umschalter wie die Theme-Wahl.</summary>
public sealed class BoolToAccentAppearanceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? ControlAppearance.Primary : ControlAppearance.Secondary;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
