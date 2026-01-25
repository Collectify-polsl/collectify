using System;
using System.Globalization;
using System.Windows.Data;

namespace Collectify.App.Converters;

/// <summary>
/// Converts an Enum value to a boolean, checking if it matches the parameter.
/// </summary>
public class EnumToBoolConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => (bool)value
            ? Enum.Parse(targetType, parameter!.ToString()!)
            : Binding.DoNothing;
}
