using Collectify.Model.Enums;

namespace Collectify.Model.InputModels;

/// <summary>
/// Describes a single field definition used when creating or modifying a template.
/// </summary>
public class TemplateFieldDefinitionInput
{
    public string Name { get; set; } = string.Empty;

    public FieldType FieldType { get; set; }

    public bool IsList { get; set; }
}