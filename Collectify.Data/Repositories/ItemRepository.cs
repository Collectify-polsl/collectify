using Collectify.Model.Collection;
using Collectify.Model.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Collectify.Data.Repositories;

/// <summary>
/// Repository for managing Item entities.
/// </summary>
public class ItemRepository : EfRepository<Item>, IItemRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ItemRepository(CollectifyContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Item>> GetByCollectionIdAsync(
        int collectionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.CollectionId == collectionId)
            .Include(i => i.FieldValues)
            .ToListAsync(cancellationToken);
    }
}