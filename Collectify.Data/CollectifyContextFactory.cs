using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Collectify.Data;

/// <summary>
/// Design time factory for EF Core tools. Used only by migrations, not at application runtime.
/// </summary>
public class CollectifyContextFactory : IDesignTimeDbContextFactory<CollectifyContext>
{
    public CollectifyContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CollectifyContext>();

        optionsBuilder.UseSqlite("Data Source=collectify.db");

        return new CollectifyContext(optionsBuilder.Options);
    }
}
