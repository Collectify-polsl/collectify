using System;
using System.Data;
using System.Globalization;
using System.Windows.Data;
using static Collectify.App.ViewModels.SingleCollectionViewModel;

namespace Collectify.App.Converters;

public class RowValueConverter : IValueConverter, IMultiValueConverter
{
    // Metoda dla MultiBinding (TextBlock.Text)
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is DataRowView row && values[1] is string columnName)
        {
            var val = row[columnName];
            if (val is ReferenceValue) return ""; // Zwracamy pusty string, bo przycisk go zastąpi
            return val?.ToString() ?? "-";
        }
        return "-";
    }

    // Metoda dla zwykłego Bindingu (Triggers)
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Jeśli trigger poda mu samą wartość komórki (np. "-")
        if (value is ReferenceValue) return "ReferenceValue";
        return value?.ToString() ?? "-";
    }

    // Metody ConvertBack (wymagane przez interfejsy)
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
