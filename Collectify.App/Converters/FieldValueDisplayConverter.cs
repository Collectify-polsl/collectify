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
            if (fv.FieldDefinition != null)
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
            
            // Fallback if FieldDefinition is null (e.g. lazy loading issue)
            if (fv.TextValue != null) return fv.TextValue;
            if (fv.IntValue.HasValue) return fv.IntValue.Value.ToString();
            if (fv.DecimalValue.HasValue) return fv.DecimalValue.Value.ToString();
            if (fv.DateValue.HasValue) return fv.DateValue.Value.ToString("dd/MM/yyyy");
            if (fv.RelatedItemId.HasValue) return $"Item #{fv.RelatedItemId}";
            if (fv.ImageValue != null && fv.ImageValue.Length > 0) return "[Image]";
            
            return "-";
        }
        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}