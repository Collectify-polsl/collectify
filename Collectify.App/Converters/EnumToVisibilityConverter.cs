using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Collectify.App.Converters;

/// <summary>
/// Converts an Enum value to Visibility based on equality with the parameter.
/// </summary>
public class EnumToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString()
            ? Visibility.Visible
            : Visibility.Collapsed;

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
