using Collectify.Model.Collection;
using Collectify.Model.InputModels;

namespace Collectify.Model.Interfaces;

/// <summary>
/// Application service responsible for creating and querying items with their field values.
/// </summary>
public interface IItemService
{
    /// <summary>
    /// Creates a new item in the given collection with the provided field values.
    /// </summary>
    Task<Item> CreateItemAsync(int collectionId, IReadOnlyList<NewItemFieldValueInput> fieldValues, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all items that belong to a specific collection.
    /// </summary>
    Task<IReadOnlyList<Item>> GetItemsForCollectionAsync(int collectionId, string? search = null, int? sortByFieldDefinitionId = null, 
        bool descending = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces all field values of the item with the provided ones.
    /// </summary>
    Task<Item> UpdateItemAsync(int itemId, IReadOnlyList<NewItemFieldValueInput> fieldValues, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a single item and its field values.
    /// </summary>
    Task DeleteItemAsync(int itemId, CancellationToken cancellationToken = default);
}