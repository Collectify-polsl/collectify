using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;

namespace Collectify.Model.Interfaces;

public interface ITemplateService
{
    Task<Template> CreateTemplateAsync(string name, IReadOnlyList<TemplateFieldDefinitionInput> fields,
        CancellationToken cancellationToken = default);

    Task UpdateTemplateAsync(int templateId, string name, CancellationToken cancellationToken = default);

    Task<FieldDefinition> AddFieldAsync(int templateId, string name, FieldType fieldType, bool isList,
        CancellationToken cancellationToken = default);

    Task UpdateFieldAsync(int fieldDefinitionId, bool isList, CancellationToken cancellationToken = default);

    Task RemoveFieldAsync(int fieldDefinitionId, CancellationToken cancellationToken = default);

    Task<Template?> GetTemplateAsync(int templateId, bool includeFields = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Template>> GetAllTemplatesAsync(string? search = null, bool sortDescending = false,
        CancellationToken cancellationToken = default);

    Task DeleteTemplateAsync(int templateId, CancellationToken cancellationToken = default);

    Task<Template?> GetTemplateByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FieldDefinition>> GetFieldDefinitionsAsync(int templateId, CancellationToken cancellationToken = default);
}