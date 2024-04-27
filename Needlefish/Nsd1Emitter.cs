using Needlefish.Compile;
using Needlefish.Lexing;
using Needlefish.Schema;
using System;
using System.Collections.Generic;

namespace Needlefish;

public class Nsd1Emitter
{
    public struct Source(string name, string content)
    {
        public string Name = name;
        public string Content = content;
    }

    private static readonly List<TokenDefinition<TokenType>> TokenDefinitions = new()
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

    private object Lock = new object();
    private Lexer<TokenType> Lexer = new(TokenDefinitions);
    private NsdParser Parser = new();
    private INsdCompiler Compiler;

    public Nsd1Emitter()
    {
        Compiler = new Nsd1Compiler();
    }

    public string Emit(string name, string source)
    {
        lock (Lock)
        {
            List<Token<TokenType>> tokens = Lexer.Lex(source);
            Nsd nsd = Parser.Parse(tokens);
            return Compiler.Compile(nsd, name);
        }
    }

    public string[] Emit(Source[] sources)
    {
        string[] results = new string[sources.Length];

        lock (Lock)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                Source source = sources[i];
                List<Token<TokenType>> tokens = Lexer.Lex(source.Content);
                Nsd nsd = Parser.Parse(tokens);
                results[i] = Compiler.Compile(nsd, source.Name);
            }
        }

        return results;
    }
}
