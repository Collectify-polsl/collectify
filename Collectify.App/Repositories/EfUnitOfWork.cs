using Collectify.Data;
using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Collectify.Model.Interfaces;

namespace Collectify.App.Repositories;

/// <summary>
/// Entity Framework implementation of the Unit of Work pattern.
/// Coordinates multiple repositories under a single database context, allowing all operations to be saved in one atomic transaction.
/// </summary>
public class EfUnitOfWork : IUnitOfWork
{
    private readonly CollectifyContext _context;

    /// <summary>
    /// Creates a new instance of the Unit of Work using the provided database context. Initializes repositories for all supported entity types.
    /// </summary>
    /// <param name="context">Entity Framework database context shared by all repositories.</param>
    public EfUnitOfWork(CollectifyContext context)
    {
        _context = context;
        Templates = new EfRepository<Template>(context);
        Collections = new EfRepository<Collection>(context);
        Items = new EfRepository<Item>(context);
        FieldDefinitions = new EfRepository<FieldDefinition>(context);
        FieldValues = new EfRepository<FieldValue>(context);
    }

    /// <summary>
    /// Repository handling operations on Template entities.
    /// </summary>
    public IRepository<Template> Templates { get; }

    /// <summary>
    /// Repository handling operations on Collection entities.
    /// </summary>
    public IRepository<Collection> Collections { get; }

    /// <summary>
    /// Repository handling operations on Item entities.
    /// </summary>
    public IRepository<Item> Items { get; }

    /// <summary>
    /// Repository handling operations on FieldDefinition entities.
    /// </summary>
    public IRepository<FieldDefinition> FieldDefinitions { get; }

    /// <summary>
    /// Repository handling operations on FieldValue entities.
    /// </summary>
    public IRepository<FieldValue> FieldValues { get; }

    ITemplateRepository IUnitOfWork.Templates => throw new NotImplementedException();
    ICollectionRepository IUnitOfWork.Collections => throw new NotImplementedException();
    IItemRepository IUnitOfWork.Items => throw new NotImplementedException();

    /// <summary>
    /// Saves all pending changes made through repositories to the database.
    /// </summary>
    /// <returns>Number of affected records.</returns>
    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    /// <summary>
    /// Disposes the underlying Entity Framework context.
    /// </summary>
    public void Dispose() => _context.Dispose();

    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}