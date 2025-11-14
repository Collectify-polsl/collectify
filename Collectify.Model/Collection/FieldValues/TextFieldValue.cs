namespace Collectify.Model.Collection;

public class TextFieldValue : FieldValue
{
    public string? Value { get; set; }

    public override object? GetValue() => Value;
    public override void SetValue(object? value) => Value = (string?)value;
}