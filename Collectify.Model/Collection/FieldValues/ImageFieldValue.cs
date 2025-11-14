namespace Collectify.Model.Collection;

public class ImageFieldValue : FieldValue
{
    public byte[]? Value { get; set; }

    public override object? GetValue() => Value;
    public override void SetValue(object? value) => Value = (byte[]?)value;
}