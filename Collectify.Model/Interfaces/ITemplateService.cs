using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;

namespace Collectify.Model.Interfaces;

/// <summary>
/// Application service responsible for managing templates and their field definitions.
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Creates a new template with the given name and field definitions.
    /// </summary>
    Task<Template> CreateTemplateAsync(string name, IReadOnlyList<TemplateFieldDefinitionInput> fields, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates only the basic metadata of the template (name).
    /// </summary>
    Task UpdateTemplateAsync(int templateId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a single field definition to an existing template.
    /// </summary>
    Task<FieldDefinition> AddFieldAsync(int templateId, string name, FieldType fieldType, bool isList,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a field definition from the system.
    /// </summary>
    Task RemoveFieldAsync(int fieldDefinitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a template optionally including its field definitions.
    /// </summary>
    Task<Template?> GetTemplateAsync(int templateId, bool includeFields = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all templates (without fields).
    /// </summary>
    Task<IReadOnlyList<Template>> GetAllTemplatesAsync(string? search = null, bool sortDescending = false, 
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Deletes a template if not used by any collection.
    /// </summary>
    Task DeleteTemplateAsync(int templateId, CancellationToken cancellationToken = default);
}