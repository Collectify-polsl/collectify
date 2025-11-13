using Collectify.Model.Enums;

namespace Collectify.Model.Entities;

/// <summary>
/// Represents a value of a specific field for a given item.
/// </summary>
public class FieldValue
{
    /// <summary>
    /// Unique identifier of the field value.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Identifier of the item that owns this value.
    /// </summary>
    public int ItemId { get; set; }

    /// <summary>
    /// Navigation property to the owning item.
    /// </summary>
    public Item Item { get; set; } = null!;

    /// <summary>
    /// Identifier of the field definition this value refers to.
    /// </summary>
    public int FieldDefinitionId { get; set; }

    /// <summary>
    /// Navigation property to the field definition.
    /// </summary>
    public FieldDefinition FieldDefinition { get; set; } = null!;

    /// <summary>
    /// Text value for text fields.
    /// </summary>
    public string? TextValue { get; set; }

    /// <summary>
    /// Integer value for integer fields.
    /// </summary>
    public int? IntegerValue { get; set; }

    /// <summary>
    /// Decimal value for decimal fields, for example prices.
    /// </summary>
    public decimal? DecimalValue { get; set; }

    /// <summary>
    /// Date value for date fields.
    /// </summary>
    public DateTime? DateValue { get; set; }

    /// <summary>
    /// Binary value for image fields, stored as bytes.
    /// </summary>
    public byte[]? ImageValue { get; set; }

    /// <summary>
    /// Convenience property that exposes the field type of this value.
    /// </summary>
    public FieldType FieldType => FieldDefinition.FieldType;
}