namespace Collectify.Model.Collection;

public class ListFieldValue : FieldValue
{
    public FieldValue[]? Value { get; set; }

    public override object? GetValue() => Value;
    public override void SetValue(object? value) => Value = (FieldValue[]?)value;
}