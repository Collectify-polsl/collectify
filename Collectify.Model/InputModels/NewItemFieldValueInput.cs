namespace Collectify.Model.InputModels;

/// <summary>
/// Describes a single field value when creating a new item.
/// Only the property corresponding to the field type is expected to be set.
/// </summary>
public class NewItemFieldValueInput
{
    /// <summary>
    /// The ID of the field definition this value corresponds to.
    /// </summary>
    public int FieldDefinitionId { get; set; }

    /// <summary>
    /// Value for text fields.
    /// </summary>
    public string? TextValue { get; set; }

    /// <summary>
    /// Value for integer fields.
    /// </summary>
    public int? IntValue { get; set; }

    /// <summary>
    /// Value for decimal fields.
    /// </summary>
    public decimal? DecimalValue { get; set; }

    /// <summary>
    /// Value for date fields.
    /// </summary>
    public DateTime? DateValue { get; set; }

    /// <summary>
    /// Value for image fields (byte array).
    /// </summary>
    public byte[]? ImageValue { get; set; }

    /// <summary>
    /// ID of the related item for reference fields.
    /// </summary>
    public int? RelatedItemId { get; set; }
}