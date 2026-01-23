using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Collectify.App.Converters
{
    // Converts boolean values to WPF Visibility constants to toggle UI element presence.
    public class BoolToVisibilityConverter : IValueConverter
    {
        // Maps a true value to Visible and false/null to Collapsed.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b) return Visibility.Visible;
            return Visibility.Collapsed;
        }

        // Translates Visibility back to a boolean by checking if the element is Visible.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v) return v == Visibility.Visible;
            return false;
        }
    }
}