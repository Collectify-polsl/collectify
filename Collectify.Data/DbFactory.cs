using Microsoft.EntityFrameworkCore;

namespace Collectify.Data;

public static class DbFactory
{
    private const string ConnectionString = "Data Source=collectify.db";

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