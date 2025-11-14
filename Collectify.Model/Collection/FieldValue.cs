namespace Collectify.Model.Collection;

using Collectify.Model.Entities;
using Collectify.Model.Enums;

/// <summary>
/// Represents the value for a specific field definition on an item.
/// </summary>
public abstract class FieldValue
{
    /// <summary>
    /// Unique identifier of the field value.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Navigation property to the field definition.
    /// </summary>
    public required FieldDefinition FieldDefinition { get; set; }

    /// <summary>
    /// Convenience property that exposes the field type of this value.
    /// </summary>
    public FieldType FieldType => FieldDefinition.FieldType;

    /// <summary>
    /// Gets the value of this field as a raw object.
    /// </summary>
    public abstract object? GetValue();

    /// <summary>
    /// Sets the value of this field from a raw object.
    /// </summary>
    public abstract void SetValue(object? value);
}