using Collectify.Model.Collection;
using Collectify.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Collectify.Data;

public class CollectifyContext(DbContextOptions<CollectifyContext> options) : DbContext(options)
{
    public DbSet<Template> Templates { get; set; } = null!;

    public DbSet<FieldDefinition> FieldDefinitions { get; set; } = null!;

    public DbSet<Collection> Collections { get; set; } = null!;

    public DbSet<Item> Items { get; set; } = null!;

    public DbSet<FieldValue> Values { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureTemplate(modelBuilder);
        ConfigureFieldDefinition(modelBuilder);
        ConfigureCollection(modelBuilder);
        ConfigureItem(modelBuilder);
        ConfigureFieldValues(modelBuilder);
    }

    private static void ConfigureTemplate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Template>(entity =>
           {
               entity.HasKey(x => x.Id);

               entity.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(200);

               entity.HasMany(x => x.Fields)
               .WithOne()
               .HasForeignKey("TemplateId")
               .OnDelete(DeleteBehavior.Cascade);
           }
        );
    }

    private static void ConfigureFieldDefinition(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FieldDefinition>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

            entity.Property(x => x.FieldType)
            .IsRequired();
        });
    }

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
            .HasForeignKey(x=> x.NextItemId)
            .OnDelete(DeleteBehavior.Restrict);
        });
    }

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
            .OnDelete(DeleteBehavior.Restrict);
        });
    }
}