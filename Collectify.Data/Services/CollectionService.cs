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

    public async Task<IReadOnlyList<CCollection>> GetCollectionsAsync(int? templateId = null, string? search = null, bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CCollection> collections = await _unitOfWork.Collections.GetAllAsync(cancellationToken);

        IEnumerable<CCollection> query = collections;

        if (templateId is not null)
            query = query.Where(c => c.TemplateId == templateId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();

            query = query.Where(c =>
                (!string.IsNullOrEmpty(c.Name) && c.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                ||
                (!string.IsNullOrEmpty(c.Description) && c.Description.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        query = sortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name);

        return query.ToList();
    }

    public async Task<CCollection?> GetCollectionAsync(int collectionId, bool includeItems = false, CancellationToken cancellationToken = default)
    {
        if (includeItems)
        {
            IReadOnlyList<CCollection> all = await _unitOfWork.Collections.GetWithItemsAsync(cancellationToken);

            return all.FirstOrDefault(c => c.Id == collectionId);
        }

        return await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);
    }

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

    public async Task DeleteCollectionAsync(int collectionId, CancellationToken cancellationToken = default)
    {
        CCollection? collection = await _unitOfWork.Collections.GetByIdAsync(collectionId, cancellationToken);

        if (collection is null)
            return;

        _unitOfWork.Collections.Remove(collection);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}