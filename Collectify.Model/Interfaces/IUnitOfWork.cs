using Collectify.Model.Collection;
using Collectify.Model.Entities;

namespace Collectify.Model.Interfaces;

/// <summary>
/// Represents a unit of work that groups multiple repository operations into a single transaction.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>Repository for template entities.</summary>
    ITemplateRepository Templates { get; }

    /// <summary>Repository for collection entities.</summary>
    ICollectionRepository Collections { get; }

    /// <summary>Repository for item entities.</summary>
    IItemRepository Items { get; }

    /// <summary>Repository for field definition entities.</summary>
    IRepository<FieldDefinition> FieldDefinitions { get; }

    /// <summary>Repository for field value entities.</summary>
    IRepository<FieldValue> FieldValues { get; }

    /// <summary>
    /// Asynchronously saves all pending changes to the underlying store.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}