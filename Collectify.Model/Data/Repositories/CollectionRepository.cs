using Collectify.Model.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Collectify.Model.Data.Repositories;

/// <summary>
/// Repository implementation for Collection entities.
/// </summary>
public class CollectionRepository : Repository<Collection.Collection>, ICollectionRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionRepository"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    public CollectionRepository(CollectifyDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Collection.Collection>> GetWithItemsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Items)
            .ToListAsync(cancellationToken);
    }
}
