using Collectify.Model.Entities;

namespace Collectify.Model.Interfaces;

/// <summary>
/// Repository interface dedicated to <see cref="Item"/> entities.
/// </summary>
public interface IItemRepository : IRepository<Item>
{
    /// <summary>
    /// Asynchronously retrieves all items that belong to a selected collection.
    /// </summary>
    /// <param name="collectionId">Identifier of the collection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Task that results in a read only list of items.
    /// </returns>
    Task<IReadOnlyList<Item>> GetByCollectionIdAsync(int collectionId, CancellationToken cancellationToken = default);
}