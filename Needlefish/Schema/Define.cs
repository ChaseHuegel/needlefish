namespace Needlefish.Schema;

public readonly struct Define
{
    public string Key { get; }

    public string Value { get; }

    public Define(string key, string value)
    {
        Key = key;
        Value = value;
    }
}
