using Collectify.Model.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Collectify.Data.Repositories;

/// <summary>
/// Generic Entity Framework repository providing basic CRUD operations.
/// </summary>
public class EfRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly CollectifyContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public EfRepository(CollectifyContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
                           .Where(predicate)
                           .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        _dbSet.Remove(entity);
    }
}