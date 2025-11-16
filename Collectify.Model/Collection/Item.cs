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

    /// <summary>
    /// Optional reference to the previous item in a logical sequence. Null means this is the first item in that chain.
    /// </summary>
    public Item? PreviousItem { get; set; }

    /// <summary>
    /// Optional reference to the next item in a logical sequence. Null means this is the last item in that chain.
    /// </summary>
    public Item? NextItem { get; set; }
}