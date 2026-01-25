using Collectify.Model.Entities;
using Collectify.Model.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Collectify.Data.Repositories;

/// <summary>
/// Repository for managing Template entities.
/// </summary>
public class TemplateRepository : EfRepository<Template>, ITemplateRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public TemplateRepository(CollectifyContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<Template?> GetWithFieldsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
}
