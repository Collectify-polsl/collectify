using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Collectify.Model.Data;

/// <summary>
/// Database context for the Collectify application using SQLite.
/// </summary>
public class CollectifyDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectifyDbContext"/> class.
    /// </summary>
    /// <param name="options">Database context options.</param>
    public CollectifyDbContext(DbContextOptions<CollectifyDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Templates DbSet.
    /// </summary>
    public DbSet<Template> Templates => Set<Template>();

    /// <summary>
    /// Gets or sets the Collections DbSet.
    /// </summary>
    public DbSet<Collection.Collection> Collections => Set<Collection.Collection>();

    /// <summary>
    /// Gets or sets the Items DbSet.
    /// </summary>
    public DbSet<Item> Items => Set<Item>();

    /// <summary>
    /// Gets or sets the FieldDefinitions DbSet.
    /// </summary>
    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();

    /// <summary>
    /// Gets or sets the FieldValues DbSet.
    /// </summary>
    public DbSet<FieldValue> FieldValues => Set<FieldValue>();

    /// <summary>
    /// Configures the model relationships and constraints.
    /// </summary>
    /// <param name="modelBuilder">Model builder instance.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Template entity
        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired();
            entity.HasMany(t => t.Fields)
                .WithOne()
                .HasForeignKey("TemplateId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure FieldDefinition entity
        modelBuilder.Entity<FieldDefinition>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).IsRequired();
            entity.Property(f => f.FieldType).IsRequired();
            entity.Property<int>("TemplateId");
        });

        // Configure Collection entity
        modelBuilder.Entity<Collection.Collection>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired();
            entity.Property(c => c.Description);
            entity.HasOne(c => c.Template)
                .WithMany()
                .HasForeignKey("TemplateId")
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(c => c.Items)
                .WithOne(i => i.Collection)
                .HasForeignKey("CollectionId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Item entity
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.CreationDate).IsRequired();
            entity.Property<int>("CollectionId");
            
            entity.HasMany(i => i.FieldValues)
                .WithOne(fv => fv.Item)
                .HasForeignKey("ItemId")
                .OnDelete(DeleteBehavior.Cascade);

            // Configure self-referencing relationships for linked list structure
            entity.HasOne(i => i.PreviousItem)
                .WithOne(i => i.NextItem)
                .HasForeignKey<Item>("PreviousItemId")
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure FieldValue entity
        modelBuilder.Entity<FieldValue>(entity =>
        {
            entity.HasKey(fv => fv.Id);
            entity.Property<int>("ItemId");
            entity.Property<int>("FieldDefinitionId");

            entity.HasOne(fv => fv.FieldDefinition)
                .WithMany()
                .HasForeignKey("FieldDefinitionId")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(fv => fv.RelatedItem)
                .WithMany()
                .HasForeignKey("RelatedItemId")
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(fv => fv.TextValue);
            entity.Property(fv => fv.IntValue);
            entity.Property(fv => fv.DecimalValue);
            entity.Property(fv => fv.DateValue);
            entity.Property(fv => fv.ImageValue);
            entity.Property<int?>("RelatedItemId");
        });
    }
}
