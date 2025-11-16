using Collectify.Model.Data.Repositories;
using Collectify.Model.Interfaces;

namespace Collectify.Model.Data;

/// <summary>
/// Unit of Work implementation that coordinates repository operations and database transactions.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly CollectifyDbContext _context;
    private ITemplateRepository? _templates;
    private ICollectionRepository? _collections;
    private IItemRepository? _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    public UnitOfWork(CollectifyDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public ITemplateRepository Templates => _templates ??= new TemplateRepository(_context);

    /// <inheritdoc />
    public ICollectionRepository Collections => _collections ??= new CollectionRepository(_context);

    /// <inheritdoc />
    public IItemRepository Items => _items ??= new ItemRepository(_context);

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
