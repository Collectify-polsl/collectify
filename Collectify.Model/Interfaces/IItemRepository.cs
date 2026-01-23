using Collectify.Model.Collection;

namespace Collectify.Model.Interfaces;

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
    
    /// <summary>
    /// Asynchronously retrieves an item including its field values.
    /// </summary>
    /// <param name="itemId">Identifier of the item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Task that results in the item with its field values, or null if not found.
    /// </returns>
    Task<Item?> GetWithFieldValuesAsync(int itemId, CancellationToken cancellationToken = default);
}