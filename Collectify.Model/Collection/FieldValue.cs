namespace Collectify.Model.Collection;

using Collectify.Model.Entities;
using Collectify.Model.Enums;

/// <summary>
/// Represents the value for a specific field definition on an item.
/// </summary>
public class FieldValue
{
    public int Id { get; set; }

    public Item Item { get; set; } = null!;

    public FieldDefinition FieldDefinition { get; set; } = null!;

    public string? TextValue { get; set; }
    public int? IntValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public DateTime? DateValue { get; set; }
    public byte[]? ImageValue { get; set; }

    public object? GetValue() => FieldDefinition.FieldType switch
    {
        FieldType.Text => TextValue,
        FieldType.Integer => IntValue,
        FieldType.Decimal => DecimalValue,
        FieldType.Date => DateValue,
        FieldType.Image => ImageValue,
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
            default:
                throw new NotSupportedException($"Unsupported field type {FieldDefinition.FieldType}");
        }
    }
}
