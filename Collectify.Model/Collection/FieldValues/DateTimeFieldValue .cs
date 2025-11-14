namespace Collectify.Model.Collection;

public class DateTimeFieldValue : FieldValue
{
    public DateTime? Value { get; set; }

    public override object? GetValue() => Value;
    public override void SetValue(object? value) => Value = (DateTime?)value;
}