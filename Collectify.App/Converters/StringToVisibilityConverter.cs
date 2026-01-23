using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Collectify.App.Converters;

// Toggles element visibility based on whether a string contains text or is empty.
public class StringToVisibilityConverter : IValueConverter
{
    public static StringToVisibilityConverter Instance = new();

    // Returns Visible if the string has content and Collapsed if it is null or empty.
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}