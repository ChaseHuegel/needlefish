using Needlefish.Lexing;
using Needlefish.Schema;
using NUnit.Framework;
using System.Collections.Generic;

namespace Needlefish.Tests;

public class SyntaxTests
{
    public static Nsd ParseNsdContent(string content)
    {
        Lexer<TokenType> lexer = LexerTests.LexerFactory;
        List<Token<TokenType>> tokens = lexer.Lex(content);
        var parser = new NsdParser();
        return parser.Parse(tokens);
    }

    [Test]
    public void Parse()
    {
        ParseNsdContent(LexerTests.ValidNsd);
    }

    [Test]
    public void ParseNoWhitespace()
    {
        ParseNsdContent(LexerTests.ValidNsdNoWhitespace);
    }

    [TestCase("Version", "message test {}")]
    [TestCase("VersionValue", "#version; message test {}")]
    public void DocumentRequirements(string name, string content)
    {
        Assert.Throws<NsdException>(() => ParseNsdContent(content));
    }

    [TestCase("Value", "#version 1; #include")]
    [TestCase("NoNumber", "#version 1; #include 1;")]
    [TestCase("NoNamespace", "#version 1; #include name.space;")]
    [TestCase("NoAssign", "#version 1; #include = \"test.nsh\";")]
    public void IncludeRequirements(string name, string content)
    {
        Assert.Throws<NsdException>(() => ParseNsdContent(content));
    }

    [TestCase("Value", "#version 1; #namespace")]
    [TestCase("NoString", "#version 1; #include \"name.space\"")]
    [TestCase("NoNumber", "#version 1; #include 1;")]
    [TestCase("NoAssign", "#version 1; #include = name.space;")]
    public void NamespaceRequirements(string name, string content)
    {
        Assert.Throws<NsdException>(() => ParseNsdContent(content));
    }

    [TestCase("Keyword", "#version 1; test {}")]
    [TestCase("Identifier", "#version 1; message {}")]
    [TestCase("Braces", "#version 1; message test")]
    [TestCase("OpenBrace", "#version 1; message test }")]
    [TestCase("CloseBrace", "#version 1; message test {")]
    [TestCase("NoNamespace", "#version 1; message name.space.test {")]
    [TestCase("NoDuplicateTypes", "#version 1; message test {} message test {}")]
    public void TypeSyntaxRequirements(string name, string content)
    {
        Assert.Throws<NsdException>(() => ParseNsdContent(content));
    }

    [TestCase("TypeName", "#version 1; message test { val = 1; }")]
    [TestCase("Identifier", "#version 1; message test { int = 1; }")]
    [TestCase("EqualToAssign", "#version 1; message test { int val 1; }")]
    [TestCase("ValueToAssign", "#version 1; message test { int val =; }")]
    [TestCase("NonTypeIdentifier", "#version 1; message test { int int = 1; }")]
    [TestCase("UniqueIdentifier", "#version 1; message test { int val = 1; string val = 2; }")]
    [TestCase("UniqueValue", "#version 1; message test { int val = 2; string str = 2; }")]
    [TestCase("NoNamespace", "#version 1; message test { int name.space.val; }")]
    public void FieldSyntaxRequirements(string name, string content)
    {
        Assert.Throws<NsdException>(() => ParseNsdContent(content));
    }

    [TestCase("NonTypeIdentifier", "#version 1; enum test { byte; }")]
    [TestCase("UniqueIdentifier", "#version 1; enum test { val; val; }")]
    [TestCase("UniqueValue", "#version 1; enum test { val = 1; val = 1; }")]
    [TestCase("Typeless", "#version 1; enum test { byte val = 1; }")]
    [TestCase("EqualToAssign", "#version 1; enum test { val 1; }")]
    [TestCase("ValueToAssign", "#version 1; enum test { val =; }")]
    [TestCase("NoNamespace", "#version 1; enum name.space.test { }")]
    public void EnumSyntaxRequirements(string name, string content)
    {
        Assert.Throws<NsdException>(() => ParseNsdContent(content));
    }
}