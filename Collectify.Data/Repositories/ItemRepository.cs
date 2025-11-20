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
            .ToListAsync(cancellationToken);
    }
}
