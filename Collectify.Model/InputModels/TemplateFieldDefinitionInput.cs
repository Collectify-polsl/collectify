using Collectify.Model.Enums;

namespace Collectify.Model.InputModels;

/// <summary>
/// Describes a single field definition used when creating or modifying a template.
/// </summary>
public class TemplateFieldDefinitionInput
{
    /// <summary>
    /// The display name of the field.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of data this field holds.
    /// </summary>
    public FieldType FieldType { get; set; }
}