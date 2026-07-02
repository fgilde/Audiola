using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Singola;

/// <summary>Hex-Farbstring („#FF4FA3") → SolidColorBrush für die Spielerfarben.</summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(value as string ?? "#B56BFF");
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }
        catch { return Brushes.MediumPurple; }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Int ↔ RadioButton: IsChecked = (Wert == Parameter); Zurückschreiben setzt den Parameter.</summary>
public sealed class IntEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && int.TryParse(parameter as string, out var p) && i == p;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true && int.TryParse(parameter as string, out var p) ? p : Binding.DoNothing;
}

/// <summary>Voller Pfad → Dateiname (für Playlist-Einträge).</summary>
public sealed class PathToNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => System.IO.Path.GetFileNameWithoutExtension(value as string ?? "");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>true → Visible (mit InverseParameter „!" → Collapsed bei true).</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var b = value is true;
        if (Equals(parameter, "!")) b = !b;
        return b ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
