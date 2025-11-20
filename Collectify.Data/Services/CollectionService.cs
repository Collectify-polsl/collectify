using CCollection = Collectify.Model.Collection.Collection;
using Collectify.Model.Interfaces;

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

    public async Task<IReadOnlyList<CCollection>> GetCollectionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Collections.GetAllAsync(cancellationToken);
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
}
