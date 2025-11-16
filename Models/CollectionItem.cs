namespace Collectify.Api.Models;

public class CollectionItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateTime DateAdded { get; set; }
    public decimal? Value { get; set; }
}
