namespace Collectify.Model.Collection;

using Collectify.Model.Entities;
using Collectify.Model.Enums;

/// <summary>
/// Represents the value for a specific field definition on an item.
/// </summary>
public class FieldValue
{
    /// <summary>
    /// Unique identifier of this field value.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The item to which this field value belongs.
    /// </summary>
    public Item Item { get; set; } = null!;

    /// <summary>
    /// The definition describing the type and meaning of this field value.
    /// </summary>
    public FieldDefinition FieldDefinition { get; set; } = null!;

    /// <summary>
    /// Value for text-based fields (<see cref="FieldType.Text"/>).
    /// </summary>
    public string? TextValue { get; set; }

    /// <summary>
    /// Value for integer fields (<see cref="FieldType.Integer"/>).
    /// </summary>
    public int? IntValue { get; set; }

    /// <summary>
    /// Value for decimal fields (<see cref="FieldType.Decimal"/>).
    /// </summary>
    public decimal? DecimalValue { get; set; }

    /// <summary>
    /// Value for date fields (<see cref="FieldType.Date"/>).
    /// </summary>
    public DateTime? DateValue { get; set; }

    /// <summary>
    /// Value for image fields (<see cref="FieldType.Image"/>), stored as raw binary data.
    /// </summary>
    public byte[]? ImageValue { get; set; }

    /// <summary>
    /// Value for reference fields (<see cref="FieldType.ItemReference"/>).
    /// Contains a reference to another <see cref="Item"/> in a different collection.
    /// </summary>
    public Item? RelatedItem { get; set; }

    /// <summary>
    /// Returns the strongly typed value of this field based on its <see cref="FieldDefinition.FieldType"/>.
    /// </summary>
    /// <returns>The stored value as an <see cref="object"/>, or <c>null</c> if not set.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when an unsupported field type is encountered.
    /// </exception>
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

    /// <summary>
    /// Assigns a given value to the correct field storage property based on the
    /// <see cref="FieldDefinition.FieldType"/>. The provided value must match
    /// the expected type of the field (for example <see cref="string"/> for text fields,
    /// <see cref="Item"/> for reference fields).
    /// </summary>
    /// <param name="value">The value to assign, or <c>null</c> to clear the field.</param>
    /// <exception cref="NotSupportedException">
    /// Thrown when trying to assign a value to an unsupported field type.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the provided value does not match the expected type.
    /// </exception>
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