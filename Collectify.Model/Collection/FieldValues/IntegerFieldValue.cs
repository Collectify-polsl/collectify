namespace Collectify.Model.Collection;

public class IntegerFieldValue : FieldValue
{
    public int? Value { get; set; }

    public override object? GetValue() => Value;
    public override void SetValue(object? value) => Value = (int?)value;
}