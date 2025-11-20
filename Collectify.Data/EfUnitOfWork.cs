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
        Collections = new CollectionRepository(context);
        Items = new ItemRepository(context);
        FieldDefinitions = new EfRepository<FieldDefinition>(context);
        FieldValues = new EfRepository<FieldValue>(context);
    }

    public ITemplateRepository Templates { get; }

    public ICollectionRepository Collections { get; }

    public IItemRepository Items { get; }

    public IRepository<FieldDefinition> FieldDefinitions { get; }

    public IRepository<FieldValue> FieldValues { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
}