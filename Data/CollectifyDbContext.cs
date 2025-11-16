using Microsoft.EntityFrameworkCore;
using Collectify.Api.Models;

namespace Collectify.Api.Data;

public class CollectifyDbContext : DbContext
{
    public CollectifyDbContext(DbContextOptions<CollectifyDbContext> options)
        : base(options)
    {
    }

    public DbSet<CollectionItem> CollectionItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the CollectionItem entity
        modelBuilder.Entity<CollectionItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Value).HasColumnType("decimal(18,2)");
        });

        // Seed some sample data
        modelBuilder.Entity<CollectionItem>().HasData(
            new CollectionItem
            {
                Id = 1,
                Name = "Sample Item 1",
                Description = "This is a sample collection item",
                Category = "Books",
                DateAdded = DateTime.UtcNow,
                Value = 25.99m
            },
            new CollectionItem
            {
                Id = 2,
                Name = "Sample Item 2",
                Description = "Another sample item",
                Category = "Coins",
                DateAdded = DateTime.UtcNow,
                Value = 150.00m
            }
        );
    }
}
