using System.Linq.Expressions;

namespace Collectify.Model.Interfaces;

/// <summary>
/// Defines a generic repository interface for basic data operations
/// on a given entity type.
/// </summary>
/// <typeparam name="TEntity">
/// Entity type that the repository works with.
/// </typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Asynchronously retrieves an entity by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Task that results in the entity instance or null if not found.
    /// </returns>
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves all entities of the given type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Task that results in a read only list of entities.
    /// </returns>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously finds entities that match the provided predicate.
    /// </summary>
    /// <param name="predicate">
    /// Expression that describes the filtering condition.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Task that results in a read only list of matching entities.
    /// </returns>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">Entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an existing entity as modified.
    /// </summary>
    /// <param name="entity">Entity to update.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">Entity to remove.</param>
    void Remove(TEntity entity);
}