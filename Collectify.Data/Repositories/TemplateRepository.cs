using Collectify.Model.Entities;
using Collectify.Model.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Collectify.Data.Repositories;

public class TemplateRepository : EfRepository<Template>, ITemplateRepository
{
    public TemplateRepository(CollectifyContext context) : base(context) { }

    public async Task<Template?> GetWithFieldsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
}
