using Collectify.Model.Collection;
using Collectify.Model.Enums;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Collectify.App.Converters;

// Formats dynamic field values into human-readable strings based on their specific data type.
public class FieldValueDisplayConverter : IValueConverter
{
    // Evaluates the field type and converts the corresponding value property into a formatted text representation.
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not FieldValue fv)
            return "-";

        return fv.FieldDefinition.FieldType switch
        {
            FieldType.Text => fv.TextValue,
            FieldType.Integer => fv.IntValue?.ToString(),
            FieldType.Decimal => fv.DecimalValue?.ToString(),
            FieldType.Date => fv.DateValue?.ToString("dd/MM/yyyy"),

            // FIX: when IsList, display the *ids* from the join table so it doesn't appear empty after reload.
            FieldType.ItemReference => fv.FieldDefinition.IsList
                ? (fv.References != null && fv.References.Count > 0
                    ? string.Join(", ", fv.References.Select(r => $"Item #{r.RelatedItemId}"))
                    : "-")
                : (fv.RelatedItemId.HasValue ? $"Item #{fv.RelatedItemId}" : "-"),

            FieldType.Image => "[Image]",
            _ => "-"
        } ?? "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}