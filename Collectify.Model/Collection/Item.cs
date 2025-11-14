namespace Collectify.Model.Collection;

/// <summary>
/// Represents a single item that belongs to a collection.
/// </summary>
public class Item
{
    /// <summary>
    /// Unique identifier of the item.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Date and time when the item was created.
    /// </summary>
    public DateTime CreationDate { get; set; }


    /// <summary>
    /// Navigation property to the owning collection.
    /// </summary>
    public Collection Collection { get; set; } = null!;

    /// <summary>
    /// Field values that describe this item.
    /// </summary>
    public ICollection<FieldValue> FieldValues { get; set; } = new List<FieldValue>();
}
