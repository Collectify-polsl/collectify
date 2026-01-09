using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;
namespace Collectify.App.Converters
{
    public class TypeToNameConverter : IValueConverter, IMultiValueConverter
    {
        // Dla MultiBinding (Triggers)
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is DataRowView row && values[1] is string colName)
            {
                return row[colName]?.GetType().Name ?? "null";
            }
            return "null";
        }

        // Dla zwykłego Bindingu
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.GetType().Name ?? "null";
        }

        public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) => throw new NotImplementedException();
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
