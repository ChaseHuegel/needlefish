namespace Needlefish.Lexing;

internal readonly struct Match<T>
{
    public readonly bool Success;
    public readonly int Length;
    public readonly Token<T> Token;

    public Match(bool success, int length, Token<T> token)
    {
        Success = success;
        Length = length;
        Token = token;
    }
}