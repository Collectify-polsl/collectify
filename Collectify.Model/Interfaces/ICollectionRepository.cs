using Collectify.Model.Collection;

namespace Collectify.Model.Interfaces;

/// <summary>
///     Repository interface dedicated to <see cref="Collection" /> entities.
/// </summary>
public interface ICollectionRepository : IRepository<Collection.Collection>
{
    /// <summary>
    ///     Asynchronously retrieves all collections including their items.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     Task that results in a read only list of collections with items.
    /// </returns>
    Task<IReadOnlyList<Collection.Collection>> GetWithItemsAsync(CancellationToken cancellationToken = default);
}