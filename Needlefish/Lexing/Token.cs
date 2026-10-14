namespace Needlefish.Lexing;

internal readonly struct Token<T>
{
    public readonly T Type;
    public readonly string Value;
    public readonly int Position;
    public readonly int Length;

    public Token(T type, string value, int position = -1, int length = 0)
    {
        Type = type;
        Value = value;
        Position = position;
        Length = length;
    }
}
