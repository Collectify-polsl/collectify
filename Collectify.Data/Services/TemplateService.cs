using Collectify.Model.Entities;
using Collectify.Model.Enums;
using Collectify.Model.InputModels;
using Collectify.Model.Interfaces;

namespace Collectify.Data.Services;

/// <summary>
/// EF Core implementation of ITemplateService.
/// </summary>
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
        Template template = new Template
        {
            Name = name
        };

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

    public async Task<FieldDefinition> AddFieldAsync(int templateId, string name, FieldType fieldType, bool isList, 
        CancellationToken cancellationToken = default)
    {
        Template? template = await _unitOfWork.Templates
            .GetByIdAsync(templateId, cancellationToken);

        if (template is null)
            throw new InvalidOperationException($"Template with id {templateId} was not found.");

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

    public async Task RemoveFieldAsync(int fieldDefinitionId, CancellationToken cancellationToken = default)
    {
        FieldDefinition? field = await _unitOfWork.FieldDefinitions
            .GetByIdAsync(fieldDefinitionId, cancellationToken);

        if (field is null)
            return;

        _unitOfWork.FieldDefinitions.Remove(field);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Template?> GetTemplateAsync(int templateId, bool includeFields = false, CancellationToken cancellationToken = default)
    {
        if (includeFields)
            return await _unitOfWork.Templates.GetWithFieldsAsync(templateId, cancellationToken);

        return await _unitOfWork.Templates.GetByIdAsync(templateId, cancellationToken);
    }

    public async Task<IReadOnlyList<Template>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Templates.GetAllAsync(cancellationToken);
    }
}