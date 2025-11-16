using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Collectify.Model.Data;

/// <summary>
/// Design-time factory for creating CollectifyDbContext instances.
/// This is used by EF Core tools for migrations.
/// </summary>
public class CollectifyDbContextFactory : IDesignTimeDbContextFactory<CollectifyDbContext>
{
    /// <summary>
    /// Creates a new instance of CollectifyDbContext for design-time operations.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>A configured CollectifyDbContext instance.</returns>
    public CollectifyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CollectifyDbContext>();
        
        // Use a default database path for migrations
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Collectify", "collectify.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new CollectifyDbContext(optionsBuilder.Options);
    }
}
