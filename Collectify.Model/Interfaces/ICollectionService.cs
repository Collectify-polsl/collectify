using CCollection = Collectify.Model.Collection.Collection;

namespace Collectify.Model.Interfaces;

/// <summary>
/// Application service responsible for managing collections based on templates.
/// </summary>
public interface ICollectionService
{
    /// <summary>
    /// Creates a new collection based on a given template.
    /// </summary>
    Task<CCollection> CreateCollectionAsync(int templateId, string name, string? description, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all collections without their items.
    /// </summary>
    Task<IReadOnlyList<CCollection>> GetCollectionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single collection optionally including its items.
    /// </summary>
    Task<CCollection?> GetCollectionAsync(int collectionId, bool includeItems = false, CancellationToken cancellationToken = default);
}