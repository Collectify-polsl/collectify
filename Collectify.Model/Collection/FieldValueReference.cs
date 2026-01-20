using Collectify.Model.Entities;

namespace Collectify.Model.Collection;

public class FieldValueReference
{
    public int Id { get; set; }

    public int FieldValueId { get; set; }
    public FieldValue FieldValue { get; set; } = null!;

    public int RelatedItemId { get; set; }
    public Item RelatedItem { get; set; } = null!;
}