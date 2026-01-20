using System.Collections.Generic;
using Collectify.Model.Entities;
using Collectify.Model.Enums;

namespace Collectify.Model.Collection;

public class FieldValue
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public int FieldDefinitionId { get; set; }
    public FieldDefinition FieldDefinition { get; set; } = null!;

    public string? TextValue { get; set; }
    public int? IntValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public DateTime? DateValue { get; set; }
    public byte[]? ImageValue { get; set; }

    public Item? RelatedItem { get; set; }
    public int? RelatedItemId { get; set; }

    public ICollection<FieldValueReference> References { get; set; } = new List<FieldValueReference>();

    public object? GetValue() => FieldDefinition.FieldType switch
    {
        FieldType.Text => TextValue,
        FieldType.Integer => IntValue,
        FieldType.Decimal => DecimalValue,
        FieldType.Date => DateValue,
        FieldType.Image => ImageValue,
        FieldType.ItemReference => RelatedItem,
        _ => throw new NotSupportedException($"Unsupported field type {FieldDefinition.FieldType}")
    };

    public void SetValue(object? value)
    {
        switch (FieldDefinition.FieldType)
        {
            case FieldType.Text:
                TextValue = (string?)value;
                break;
            case FieldType.Integer:
                IntValue = value is null ? null : Convert.ToInt32(value);
                break;
            case FieldType.Decimal:
                DecimalValue = value is null ? null : Convert.ToDecimal(value);
                break;
            case FieldType.Date:
                DateValue = (DateTime?)value;
                break;
            case FieldType.Image:
                ImageValue = (byte[]?)value;
                break;
            case FieldType.ItemReference:
                if (value is null || value is Item)
                    RelatedItem = (Item?)value;
                else
                    throw new ArgumentException("ItemReference fields expect a value of type Item or null.");
                break;
            default:
                throw new NotSupportedException($"Unsupported field type {FieldDefinition.FieldType}");
        }
    }
}