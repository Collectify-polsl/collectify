using Collectify.Model.Collection;
using Collectify.Model.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Collectify.Data.Repositories;

public class CollectionRepository : EfRepository<Collection>, ICollectionRepository
{
    public CollectionRepository(CollectifyContext context) : base(context) { }

    public override async Task<IReadOnlyList<Collection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Template)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Collection>> GetWithItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Items)
            .ToListAsync(cancellationToken);
    }
}
