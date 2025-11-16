using Collectify.Model.Entities;

namespace Collectify.Model.Collection;

/// <summary>
/// Represents a user collection based on a specific template.
/// </summary>
public class Collection
{
    /// <summary>
    /// Unique identifier of the collection.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name of the collection.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the collection.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Foreign key of the template used by this collection.
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Navigation property to the template used by this collection.
    /// </summary>
    public Template Template { get; set; } = null!;

    /// <summary>
    /// Items that belong to this collection.
    /// </summary>
    public List<Item> Items { get; set; } = [];
}
