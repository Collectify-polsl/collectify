namespace Collectify.Model.InputModels;

/// <summary>
/// Describes a single field value when creating a new item.
/// Only the property corresponding to the field type is expected to be set.
/// </summary>
public class NewItemFieldValueInput
{
    public int FieldDefinitionId { get; set; }

    public string? TextValue { get; set; }

    public int? IntValue { get; set; }

    public decimal? DecimalValue { get; set; }

    public DateTime? DateValue { get; set; }

    public bool? BoolValue { get; set; }

    public byte[]? ImageValue { get; set; }

    public int? RelatedItemId { get; set; }
}