namespace Domain.ValueObjects;

public sealed record Barcode
{
    private readonly string _value = "";

    public string Value
    {
        get => _value;
        init => _value = !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException("Barcode value cannot be empty.", nameof(Value));
    }

    public Barcode(string Value)
    {
        this.Value = Value;
    }
}
