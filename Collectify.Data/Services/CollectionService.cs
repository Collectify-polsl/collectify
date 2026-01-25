using Collectify.Model.Entities;
using Collectify.Model.Interfaces;
using CCollection = Collectify.Model.Collection.Collection;

namespace Collectify.Data.Services;

/// <summary>
/// EF Core implementation of ICollectionService.
/// </summary>
public class CollectionService : ICollectionService
{
    private readonly IUnitOfWork _unitOfWork;

    public CollectionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<CCollection> CreateCollectionAsync(int templateId, string name, string? description,
        CancellationToken cancellationToken = default)
    {
        var template = await _unitOfWork.Templates.GetByIdAsync(templateId, cancellationToken);
        if (template is null)
            throw new InvalidOperationException($"Template with id {templateId} was not found.");

        CCollection collection = new CCollection
        {
            Name = name,
            Description = description,
            TemplateId = templateId
        };

        await _unitOfWork.Collections.AddAsync(collection, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return collection;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CCollection>> GetCollectionsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CCollection> collections = await _unitOfWork.Collections.GetAllAsync(cancellationToken);
        IReadOnlyList<Template> templates = await _unitOfWork.Templates.GetAllAsync(cancellationToken);

        var templateMap = templates.ToDictionary(t => t.Id);

        foreach (var collection in collections)
        {
            if (templateMap.TryGetValue(collection.TemplateId, out var template))
            {
                collection.Template = template;
            }
        }

        return collections.OrderBy(c => c.Name).ToList();
    }

    /// <inheritdoc />
    public async Task<CCollection?> GetCollectionAsync(int collectionId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateCollectionAsync(int collectionId, string name, string? description, CancellationToken cancellationToken = default)
    {
        CCollection? collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);

        if (collection is null)
            throw new InvalidOperationException($"Collection with id {collectionId} was not found.");

        collection.Name = name;
        collection.Description = description;

        _unitOfWork.Collections.Update(collection);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteCollectionAsync(int collectionId, CancellationToken cancellationToken = default)
    {
        CCollection? collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);

        if (collection is null)
            return;

        _unitOfWork.Collections.Remove(collection);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}