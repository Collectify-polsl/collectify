using Collectify.Model.Collection;
using Collectify.Model.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Collectify.Data.Repositories;

public class CollectionRepository : EfRepository<Collection>, ICollectionRepository
{
    public CollectionRepository(CollectifyContext context) : base(context) { }

    public async Task<IReadOnlyList<Collection>> GetWithItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Items)
            .ToListAsync(cancellationToken);
    }
}
