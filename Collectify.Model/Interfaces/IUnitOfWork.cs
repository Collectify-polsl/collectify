namespace Collectify.Model.Interfaces;

/// <summary>
/// Represents a unit of work that groups multiple repository operations into a single transaction.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// Repository for template entities.
    /// </summary>
    ITemplateRepository Templates { get; }

    /// <summary>
    /// Repository for collection entities.
    /// </summary>
    ICollectionRepository Collections { get; }

    /// <summary>
    /// Repository for item entities.
    /// </summary>
    IItemRepository Items { get; }

    /// <summary>
    /// Asynchronously saves all pending changes to the underlying store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Task that results in the number of affected records.
    /// </returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}