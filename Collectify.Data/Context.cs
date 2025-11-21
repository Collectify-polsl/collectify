using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Collectify.Data;

/// <summary>
/// Entity Framework database context for the Collectify application. Defines entity sets and configures their relationships and constraints.
/// </summary>
public class CollectifyContext(DbContextOptions<CollectifyContext> options) : DbContext(options)
{
    /// <summary>
    /// DbSet representing all templates stored in the database.
    /// </summary>
    public DbSet<Template> Templates { get; set; } = null!;

    /// <summary>
    /// DbSet representing all field definitions stored in the database.
    /// </summary>
    public DbSet<FieldDefinition> FieldDefinitions { get; set; } = null!;

    /// <summary>
    /// DbSet representing all collections created by users.
    /// </summary>
    public DbSet<Collection> Collections { get; set; } = null!;

    /// <summary>
    /// DbSet representing all items that belong to collections.
    /// </summary>
    public DbSet<Item> Items { get; set; } = null!;

    /// <summary>
    /// DbSet representing all field values assigned to items.
    /// </summary>
    public DbSet<FieldValue> Values { get; set; } = null!;

    /// <summary>
    /// Configures the model by applying entity specific mappings and relationships.
    /// </summary>
    /// <param name="modelBuilder">Model builder used to configure entity types.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureTemplate(modelBuilder);
        ConfigureFieldDefinition(modelBuilder);
        ConfigureCollection(modelBuilder);
        ConfigureItem(modelBuilder);
        ConfigureFieldValues(modelBuilder);
    }

    /// <summary>
    /// Configures the Template entity, including keys, properties and relationships.
    /// </summary>
    /// <param name="modelBuilder">Model builder used to configure the entity.</param>
    private static void ConfigureTemplate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

            entity.HasMany(x => x.Fields)
            .WithOne(f => f.Template)
            .HasForeignKey(f => f.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
        }
        );
    }

    /// <summary>
    /// Configures the FieldDefinition entity, including keys, properties and conversions.
    /// </summary>
    /// <param name="modelBuilder">Model builder used to configure the entity.</param>
    private static void ConfigureFieldDefinition(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FieldDefinition>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

            entity.Property(x => x.FieldType)
            .IsRequired()
            .HasConversion<string>();
        });
    }

    /// <summary>
    /// Configures the Collection entity, including keys, properties and relationships.
    /// </summary>
    /// <param name="modelBuilder">Model builder used to configure the entity.</param>
    private static void ConfigureCollection(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

            entity.Property(x => x.Description)
            .HasMaxLength(1000);

            entity.HasOne(x => x.Template)
            .WithMany()
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Items)
            .WithOne(x => x.Collection)
            .HasForeignKey(x => x.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the Item entity, including keys, properties, relationships and indexes.
    /// </summary>
    /// <param name="modelBuilder">Model builder used to configure the entity.</param>
    private static void ConfigureItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.CreationDate)
            .IsRequired();

            entity.HasMany(x => x.FieldValues)
            .WithOne(x => x.Item)
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.PreviousItem)
            .WithMany()
            .HasForeignKey(x => x.PreviousItemId)
            .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.NextItem)
            .WithMany()
            .HasForeignKey(x => x.NextItemId)
            .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.CreationDate);
            entity.HasIndex(x => x.CollectionId);
        });
    }

    /// <summary>
    /// Configures the FieldValue entity, including keys, relationships and indexes.
    /// </summary>
    /// <param name="modelBuilder">Model builder used to configure the entity.</param>
    private static void ConfigureFieldValues(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FieldValue>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Item)
            .WithMany(x => x.FieldValues)
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.FieldDefinition)
            .WithMany()
            .HasForeignKey(x => x.FieldDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.RelatedItem)
            .WithMany()
            .HasForeignKey(x => x.RelatedItemId)
            .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => new { x.ItemId, x.FieldDefinitionId })
            .IsUnique();

            entity.HasIndex(x => x.FieldDefinitionId);
        });
    }
}