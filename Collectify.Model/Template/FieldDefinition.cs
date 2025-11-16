using Collectify.Model.Enums;

namespace Collectify.Model.Entities;

/// <summary>
/// Represents a single field definition that belongs to a template.
/// </summary>
public class FieldDefinition
{
    /// <summary>
    /// Index of the field definition in this template.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name of the field, for example "Title" or "Author".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of the field, for example text or integer.
    /// </summary>
    public FieldType FieldType { get; set; }
}
