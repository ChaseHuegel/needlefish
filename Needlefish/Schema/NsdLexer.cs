using Needlefish.Lexing;
using System.Collections.Generic;

namespace Needlefish.Schema;

/// <summary>
/// Creates lexers configured with the nsd token definitions.
/// </summary>
internal static class NsdLexer
{
    public static readonly List<TokenDefinition<TokenType>> TokenDefinitions = new()
    {
        new(TokenType.Whitespace, @"\G[\s\t\n\r\f\0]+"),
        new(TokenType.Define, @"\G#"),
        new(TokenType.Number, @"\G[+-]?\d*\.?\d+"),
        new(TokenType.Terminate, @"\G;"),
        new(TokenType.Array, @"\G\[\]"),
        new(TokenType.Optional, @"\G\?"),
        new(TokenType.Equals, @"\G="),
        new(TokenType.OpenBrace, @"\G{"),
        new(TokenType.CloseBrace, @"\G}"),
        new(TokenType.Message, @"\Gmessage"),
        new(TokenType.Enum, @"\Genum"),
        new(TokenType.String, @"\Gstring"),
        new(TokenType.Int, @"\Gint"),
        new(TokenType.Float, @"\Gfloat"),
        new(TokenType.Double, @"\Gdouble"),
        new(TokenType.Long, @"\Glong"),
        new(TokenType.Uint, @"\Guint"),
        new(TokenType.Bool, @"\Gbool"),
        new(TokenType.Byte, @"\Gbyte"),
        new(TokenType.Ulong, @"\Gulong"),
        new(TokenType.Short, @"\Gshort"),
        new(TokenType.UShort, @"\Gushort"),
        new(TokenType.StringValue, @"\G""[^""]*"""),
        new(TokenType.Identifier, @"\G[a-zA-Z]*([.][a-zA-Z]|[a-zA-Z0-9_])+"),
    };

    public static Lexer<TokenType> Create()
    {
        return new Lexer<TokenType>(TokenDefinitions);
    }
}
