using Collectify.Data.Repositories;
using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Collectify.Model.Interfaces;

namespace Collectify.Data;

/// <summary>
/// Coordinates EF repositories under a single database context.
/// </summary>
public class EfUnitOfWork : IUnitOfWork
{
    private readonly CollectifyContext _context;

    public EfUnitOfWork(CollectifyContext context)
    {
        _context = context;

        Templates = new TemplateRepository(context);
        Collections = new EfRepository<Collection>(context);
        Items = new ItemRepository(context);
        FieldDefinitions = new EfRepository<FieldDefinition>(context);
        FieldValues = new EfRepository<FieldValue>(context);
    }

    /// <inheritdoc />
    public ITemplateRepository Templates { get; }

    /// <inheritdoc />
    public IRepository<Collection> Collections { get; }

    /// <inheritdoc />
    public IItemRepository Items { get; }

    /// <inheritdoc />
    public IRepository<FieldDefinition> FieldDefinitions { get; }

    /// <inheritdoc />
    public IRepository<FieldValue> FieldValues { get; }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
}