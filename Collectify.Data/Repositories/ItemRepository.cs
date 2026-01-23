using Collectify.Model.Collection;
using Collectify.Model.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Collectify.Data.Repositories;

public class ItemRepository : EfRepository<Item>, IItemRepository
{
    public ItemRepository(CollectifyContext context) : base(context) { }

    public async Task<IReadOnlyList<Item>> GetByCollectionIdAsync(
        int collectionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.CollectionId == collectionId)
            .Include(i => i.FieldValues)
                .ThenInclude(v => v.FieldDefinition)      // <-- needed for display/converters
            .Include(i => i.FieldValues)
                .ThenInclude(v => v.References)
                    .ThenInclude(r => r.RelatedItem)
            .Include(i => i.FieldValues)
                .ThenInclude(v => v.RelatedItem)
            .ToListAsync(cancellationToken);
    }

    public async Task<Item?> GetWithFieldValuesAsync(int itemId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.Id == itemId)
            .Include(i => i.FieldValues)
                .ThenInclude(v => v.FieldDefinition)      // <-- needed for display/converters
            .Include(i => i.FieldValues)
                .ThenInclude(v => v.References)
                    .ThenInclude(r => r.RelatedItem)
            .Include(i => i.FieldValues)
                .ThenInclude(v => v.RelatedItem)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Item>> GetItemsForCollectionAsync(int collectionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.CollectionId == collectionId)
            .Include(i => i.FieldValues)
                .ThenInclude(v => v.FieldDefinition)      // keep FieldDefinition for previews
            .Include(i => i.FieldValues)
                .ThenInclude(v => v.References)
                    .ThenInclude(r => r.RelatedItem)
            .Include(i => i.FieldValues)
                .ThenInclude(v => v.RelatedItem)
            .ToListAsync(cancellationToken);
    }
}