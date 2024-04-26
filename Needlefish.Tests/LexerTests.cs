using Needlefish.Lexing;
using Needlefish.Schema;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Needlefish.Tests;

public class LexerTests
{
    internal const string ValidNsdNoWhitespace = """#version 1.0;#include "header.nsd";#namespace Lexer.Tests;message TestMessage{string? Content;int StatusCode=5;float FloAT;double Double;long Long;ulong uLong;uint Uint=15;ulong Ulong;short Short;ushort UShort;bool Bool;byte Byte;byte[] Bytes=21;string[]? OptionalStrings=25;TestEnum EnumValue=22;}enum TestEnum{Val1;Val2=10;Val3;Val4;}message Submessage{int? OptionalInt;}""";

    internal const string ValidNsd = """
        #version 1.0;
        #namespace Lexer.Tests;
        #include "header.nsd";

        message TestMessage
        {
            string? Content;
            string Body;
            float FloAT;
            double Double;
            long Long;
            ulong uLong;
            ulong Ulong = 15;
            short Short;
            ushort UShort;
            bool Bool;
            byte Byte;
            byte[] Bytes = 21;
            string[]?  OptionalStrings = 30;

            int Int;
            int? OptionalInt;
            int[] Ints;
            int[]? OptionalInts;

            uint UInt;
            uint? OptionalUInt;
            uint[] UInts;
            uint[]? OptionalUInts;

            TestEnum Enum = 22;
            TestEnum? OptionalEnum;
            TestEnum[] Enums;
            TestEnum[]? OptionalEnums;

            Submessage Submessage;
            Submessage? OptionalSubmessage;
            Submessage[] Submessages;
            Submessage[]? OptionalSubmessages;
        }

        enum TestEnum {
            Val1;
            Val2 = 10;
            Val3;
            Val4;
        }

        message Submessage {
            int? OptionalInt;
        }
        """;

    private const string UnknownTokenNsd = "a + b = c;";

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

    internal static Lexer<TokenType> LexerFactory => new(TokenDefinitions);

    [Test]
    public void Lex()
    {
        List<Token<TokenType>> tokens = LexerFactory.Lex(ValidNsd);

        foreach (var token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token.Value))
            {
                Console.WriteLine($"{token.Type}");
            }
            else
            {
                Console.WriteLine($"{token.Type}: {token.Value}");
            }
        }
    }

    [Test]
    public void LexNoWhitespace()
    {
        List<Token<TokenType>> tokens = LexerFactory.Lex(ValidNsdNoWhitespace);

        foreach (var token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token.Value))
            {
                Console.WriteLine($"{token.Type}");
            }
            else
            {
                Console.WriteLine($"{token.Type}: {token.Value}");
            }
        }
    }

    [Test]
    public void UnknownTokenThrows()
    {
        FormatException ex = Assert.Throws<FormatException>(() => LexerFactory.Lex(UnknownTokenNsd));

        Assert.That(ex.Message, Contains.Substring("ln (1), col (3)"));
    }
}