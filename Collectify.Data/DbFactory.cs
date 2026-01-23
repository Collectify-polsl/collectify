using Microsoft.EntityFrameworkCore;

namespace Collectify.Data;

/// <summary>
/// Factory responsible for creating and configuring Collectify database contexts.
/// </summary>
public static class DbFactory
{
    /// <summary>
    /// Connection string used for the SQLite Collectify database file.
    /// </summary>
    private const string ConnectionString = "Data Source=collectify.db";

    /// <summary>
    /// Creates a new instance of <see cref="CollectifyContext"/> configured to use SQLite and applies any pending migrations to the database.
    /// </summary>
    /// <returns>Initialized and migrated <see cref="CollectifyContext"/> instance.</returns>
    public static CollectifyContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CollectifyContext>()
            .UseSqlite(ConnectionString)
            .Options;

        var context = new CollectifyContext(options);

        context.Database.Migrate();

        return context;
    }
}