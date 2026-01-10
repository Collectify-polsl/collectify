using Collectify.Model.Collection;
using Collectify.Model.Enums;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Collectify.App.Converters;

public class FieldValueDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FieldValue fv)
        {
            return fv.FieldDefinition.FieldType switch
            {
                FieldType.Text => fv.TextValue,
                FieldType.Integer => fv.IntValue?.ToString(),
                FieldType.Decimal => fv.DecimalValue?.ToString(),
                FieldType.Date => fv.DateValue?.ToString("dd/MM/yyyy"),
                FieldType.ItemReference => fv.RelatedItemId.HasValue ? $"Item #{fv.RelatedItemId}" : "-",
                FieldType.Image => "[Image]",
                _ => "-"
            } ?? "-";
        }
        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}