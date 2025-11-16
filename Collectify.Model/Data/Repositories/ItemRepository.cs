using Collectify.Model.Collection;
using Collectify.Model.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Collectify.Model.Data.Repositories;

/// <summary>
/// Repository implementation for Item entities.
/// </summary>
public class ItemRepository : Repository<Item>, IItemRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemRepository"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    public ItemRepository(CollectifyDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Item>> GetByCollectionIdAsync(int collectionId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => EF.Property<int>(i, "CollectionId") == collectionId)
            .ToListAsync(cancellationToken);
    }
}
