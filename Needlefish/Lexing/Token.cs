namespace Needlefish.Lexing;

internal readonly struct Token<T>
{
    public readonly T Type;
    public readonly string Value;

    public Token(T type, string value)
    {
        Type = type;
        Value = value;
    }
}