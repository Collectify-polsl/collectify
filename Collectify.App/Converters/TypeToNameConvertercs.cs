using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;
namespace Collectify.App.Converters
{
    /// <summary>
    /// Converts a value to its type name string.
    /// </summary>
    public class TypeToNameConverter : IValueConverter, IMultiValueConverter
    {
        /// <summary>
        /// Converts multi-value binding to the type name of the specified column value.
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is DataRowView row && values[1] is string colName)
            {
                return row[colName]?.GetType().Name ?? "null";
            }
            return "null";
        }

        /// <summary>
        /// Converts a single value to its type name.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.GetType().Name ?? "null";
        }

        /// <inheritdoc />
        public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) => throw new NotImplementedException();
        
        /// <inheritdoc />
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
