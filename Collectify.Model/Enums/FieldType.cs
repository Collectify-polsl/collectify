namespace Collectify.Model.Enums;

/// <summary>
/// Represents the type of a custom field defined in a template.
/// </summary>
public enum FieldType
{
    /// <summary>
    /// A text value, for example a title or description.
    /// </summary>
    Text,

    /// <summary>
    /// An integer value without fractional part.
    /// </summary>
    Integer,

    /// <summary>
    /// A decimal value, for example a price.
    /// </summary>
    Decimal,

    /// <summary>
    /// A date value, for example creation date or release date.
    /// </summary>
    Date,

    /// <summary>
    /// A binary image value stored as bytes.
    /// </summary>
    Image
}