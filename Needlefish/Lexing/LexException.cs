using System;

namespace Needlefish.Lexing;

/// <summary>
/// Thrown when the lexer encounters input it cannot tokenize. Derives from
/// <see cref="FormatException"/> for compatibility with previous behavior, and carries the
/// offset of the offending character so callers can produce a precise diagnostic.
/// </summary>
internal class LexException : FormatException
{
    public int Position { get; }

    public int Length { get; }

    public LexException(string message, int position, int length)
        : base(message)
    {
        Position = position;
        Length = length;
    }
}
