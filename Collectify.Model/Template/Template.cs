namespace Collectify.Model.Entities;

/// <summary>
/// Represents a template that defines a structure of a collection.
/// </summary>
public class Template
{
    /// <summary>
    /// Unique identifier of the template.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name of the template.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Collection of field definitions that belong to this template.
    /// </summary>
    public List<FieldDefinition> Fields { get; set; } = [];
}