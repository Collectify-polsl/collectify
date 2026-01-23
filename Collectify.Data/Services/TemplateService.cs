using System.Linq;
using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
using Collectify.Model.Interfaces;

namespace Collectify.Data.Services;

public class TemplateService : ITemplateService
{
    private readonly IUnitOfWork _unitOfWork;

    public TemplateService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Template> CreateTemplateAsync(string name, IReadOnlyList<TemplateFieldDefinitionInput> fields,
        CancellationToken cancellationToken = default)
    {
        Template template = new Template { Name = name };

        foreach (TemplateFieldDefinitionInput field in fields)
        {
            FieldDefinition definition = new FieldDefinition
            {
                Name = field.Name,
                FieldType = field.FieldType,
                IsList = field.IsList,
                Template = template
            };
            template.Fields.Add(definition);
        }

        await _unitOfWork.Templates.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return template;
    }

    public async Task UpdateTemplateAsync(int templateId, string name, CancellationToken cancellationToken = default)
    {
        Template? template = await _unitOfWork.Templates.GetByIdAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException($"Template with id {templateId} was not found.");

        template.Name = name;

        _unitOfWork.Templates.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<FieldDefinition> AddFieldAsync(int templateId, string name, FieldType fieldType, bool isList,
        CancellationToken cancellationToken = default)
    {
        Template? template = await _unitOfWork.Templates.GetByIdAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException($"Template with id {templateId} was not found.");

        FieldDefinition definition = new FieldDefinition
        {
            Name = name,
            FieldType = fieldType,
            IsList = isList,
            TemplateId = templateId
        };

        await _unitOfWork.FieldDefinitions.AddAsync(definition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return definition;
    }

    public async Task UpdateFieldAsync(int fieldDefinitionId, bool isList, CancellationToken cancellationToken = default)
    {
        FieldDefinition? field = await _unitOfWork.FieldDefinitions.GetByIdAsync(fieldDefinitionId, cancellationToken)
            ?? throw new InvalidOperationException($"Field definition with id {fieldDefinitionId} was not found.");

        field.IsList = isList;

        _unitOfWork.FieldDefinitions.Update(field);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFieldAsync(int fieldDefinitionId, CancellationToken cancellationToken = default)
    {
        FieldDefinition? field = await _unitOfWork.FieldDefinitions.GetByIdAsync(fieldDefinitionId, cancellationToken);
        if (field is null)
            return;

        _unitOfWork.FieldDefinitions.Remove(field);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Template?> GetTemplateAsync(int templateId, bool includeFields = false, CancellationToken cancellationToken = default)
        => includeFields
            ? await _unitOfWork.Templates.GetWithFieldsAsync(templateId, cancellationToken)
            : await _unitOfWork.Templates.GetByIdAsync(templateId, cancellationToken);

    public async Task<IReadOnlyList<Template>> GetAllTemplatesAsync(string? search = null, bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Template> templates = await _unitOfWork.Templates.GetAllAsync(cancellationToken);
        IEnumerable<Template> query = templates;

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(t => !string.IsNullOrEmpty(t.Name) && t.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        query = sortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name);
        return query.ToList();
    }

    public async Task DeleteTemplateAsync(int templateId, CancellationToken cancellationToken = default)
    {
        Template? template = await _unitOfWork.Templates.GetByIdAsync(templateId, cancellationToken);
        if (template is null)
            return;

        bool anyCollections = (await _unitOfWork.Collections.FindAsync(c => c.TemplateId == templateId, cancellationToken)).Any();
        if (anyCollections)
            throw new InvalidOperationException("Cannot delete a template that is used by existing collections.");

        _unitOfWork.Templates.Remove(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Template?> GetTemplateByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        string normalized = name.Trim().ToLowerInvariant();
        IReadOnlyList<Template> matches = await _unitOfWork.Templates.FindAsync(
            t => t.Name != null && t.Name.ToLower() == normalized,
            cancellationToken);

        return matches.FirstOrDefault();
    }

    public Task<IReadOnlyList<FieldDefinition>> GetFieldDefinitionsAsync(int templateId, CancellationToken cancellationToken = default)
        => _unitOfWork.FieldDefinitions.FindAsync(fd => fd.TemplateId == templateId, cancellationToken);
}