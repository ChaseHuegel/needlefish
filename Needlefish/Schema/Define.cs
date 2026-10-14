namespace Needlefish.Schema;

public readonly struct Define
{
    public string Key { get; }

    public string Value { get; }

    /// <summary>
    /// 0-based character offset of the define value in the source, or -1 when unknown.
    /// </summary>
    public int Position { get; }

    public int Length { get; }

    public Define(string key, string value, int position = -1, int length = 0)
    {
        Key = key;
        Value = value;
        Position = position;
        Length = length;
    }
}
