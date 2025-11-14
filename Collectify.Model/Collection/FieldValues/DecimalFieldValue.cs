namespace Collectify.Model.Collection;

public class DecimalFieldValue : FieldValue
{
    public decimal? Value { get; set; }

    public override object? GetValue() => Value;
    public override void SetValue(object? value) => Value = (decimal?)value;
}