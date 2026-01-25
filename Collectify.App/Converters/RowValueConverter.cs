using System;
using System.Data;
using System.Globalization;
using System.Windows.Data;
using static Collectify.App.ViewModels.SingleCollectionViewModel;

namespace Collectify.App.Converters;

/// <summary>
/// Converts row values for display in the grid.
/// </summary>
public class RowValueConverter : IValueConverter, IMultiValueConverter
{
    /// <summary>
    /// Converts multi-value binding (row and column name) to the cell value string.
    /// </summary>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is DataRowView row && values[1] is string columnName)
        {
            var val = row[columnName];
            if (val is ReferenceValue) return "";
            return val?.ToString() ?? "-";
        }
        return "-";
    }

    /// <summary>
    /// Converts a single value to its string representation.
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ReferenceValue) return "ReferenceValue";
        return value?.ToString() ?? "-";
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    
    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
