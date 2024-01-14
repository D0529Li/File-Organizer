using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace File_Organizer;

enum Parameters
{
    Normal = 0,
    Inverted,
    Inverse
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = (bool)value;
        var direction = Parameters.Normal;

        if (parameter != null)
            direction = (Parameters)Enum.Parse(typeof(Parameters), (string)parameter);

        if (direction == Parameters.Inverted || direction == Parameters.Inverse)
            return !boolValue ? Visibility.Visible : Visibility.Collapsed;

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}

[ValueConversion(typeof(bool), typeof(bool))]
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(bool)value;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}
