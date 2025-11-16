using Collectify.Model.Entities;
using Collectify.Model.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Collectify.Model.Data.Repositories;

/// <summary>
/// Repository implementation for Template entities.
/// </summary>
public class TemplateRepository : Repository<Template>, ITemplateRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateRepository"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    public TemplateRepository(CollectifyDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Template?> GetWithFieldsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
}
