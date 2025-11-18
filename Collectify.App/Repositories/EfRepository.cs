using Collectify.Data;
using Collectify.Model.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Collectify.App.Repositories;

/// <summary>
/// Generic Entity Framework repository providing basic CRUD operations for any entity type in the Collectify database.
/// </summary>
/// <typeparam name="TEntity">Type of the entity represented by this repository.</typeparam>
public class EfRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly CollectifyContext _context;
    private readonly DbSet<TEntity> _dbSet;

    /// <summary>
    /// Creates a new instance of the repository using the provided database context.
    /// </summary>
    /// <param name="context">Entity Framework database context.</param>
    public EfRepository(CollectifyContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    /// <summary>
    /// Retrieves all entities of type <typeparamref name="TEntity"/> from the database. Returned entities are not tracked by the change tracker.
    /// </summary>
    /// <returns>List of all entities.</returns>
    public async Task<List<TEntity>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    /// <summary>
    /// Retrieves an entity by its primary key identifier.
    /// </summary>
    /// <param name="id">Primary key identifier of the entity.</param>
    /// <returns>Entity instance or null if not found.</returns>
    public async Task<TEntity?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <summary>
    /// Marks an entity as added so it will be inserted during the next SaveChanges call.
    /// </summary>
    /// <param name="entity">Entity to be added.</param>
    public void Add(TEntity entity)
    {
        _dbSet.Add(entity);
    }

    /// <summary>
    /// Marks an entity as removed so it will be deleted during the next SaveChanges call.
    /// </summary>
    /// <param name="entity">Entity to be removed.</param>
    public void Remove(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    /// <inheritdoc/>
    public Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void Update(TEntity entity)
    {
        throw new NotImplementedException();
    }
}