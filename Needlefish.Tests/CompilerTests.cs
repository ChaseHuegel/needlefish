using Needlefish.Compile;
using Needlefish.Schema;
using NUnit.Framework;

namespace Needlefish.Tests;

internal class CompilerTests
{
    [Test]
    public void Compile()
    {
        Nsd nsd = SyntaxTests.ParseNsdContent(LexerTests.VALID_NSD);

        var options = new CompilerOptions()
        {
            Partial = true
        };
        var compiler = new Nsd1Compiler(options);

        string result = compiler.Compile(nsd, "LexerTests.ValidNsd");

        Assert.Pass(result);
    }

    [Test]
    public void SerializeMethodsAreReadonly()
    {
        Nsd nsd = SyntaxTests.ParseNsdContent(LexerTests.VALID_NSD);

        var compiler = new Nsd1Compiler(new CompilerOptions());
        string result = compiler.Compile(nsd, "LexerTests.ValidNsd");

        StringAssert.Contains("public readonly int GetSize()", result);
        StringAssert.Contains("public readonly byte[] Serialize()", result);
        StringAssert.Contains("public readonly unsafe int SerializeInto(byte[] buffer, int start)", result);
        StringAssert.Contains("public readonly int SerializeInto(Span<byte> buffer)", result);
        StringAssert.Contains("public readonly unsafe int SerializeInto(Span<byte> buffer, int start)", result);

        // Unpack mutates the struct and must not be readonly.
        StringAssert.Contains("public unsafe int Unpack(byte[] buffer, int start, int length)", result);
    }

    [Test]
    public void GeneratesNullableEnableDirective()
    {
        Nsd nsd = SyntaxTests.ParseNsdContent(LexerTests.VALID_NSD);

        var compiler = new Nsd1Compiler(new CompilerOptions());
        string result = compiler.Compile(nsd, "LexerTests.ValidNsd");

        StringAssert.Contains("#nullable enable", result);
    }

    [Test]
    public void ArrayBoundsCheckUsesFixedElementSize()
    {
        const string nsd = """
            #version 1.0;
            #namespace Test;
            message M
            {
                float[] Samples;
                E[] Enums;
                M[] Children;
            }
            enum E { A; }
            """;

        Nsd parsed = SyntaxTests.ParseNsdContent(nsd);
        var compiler = new Nsd1Compiler(new CompilerOptions());
        string result = compiler.Compile(parsed, "Test");

        StringAssert.Contains("offset + g__Samples_Length * 4 > end", result);
        StringAssert.Contains("offset + g__Enums_Length * 4 > end", result);
        StringAssert.DoesNotContain("g__Children_Length *", result);
    }

    [Test]
    public void SerializerArrayElementSizesAreFixedPerType()
    {
        const string nsd = """
            #version 1.0;
            #namespace Test;
            message M
            {
                float[] Floats;
                double[] Doubles;
                int[] Ints;
                byte[] Bytes;
            }
            """;

        Nsd parsed = SyntaxTests.ParseNsdContent(nsd);
        var compiler = new Nsd1Compiler(new CompilerOptions());
        string result = compiler.Compile(parsed, "Test");

        StringAssert.Contains("(long)g__Floats_Length * 4", result);
        StringAssert.Contains("(long)g__Doubles_Length * 8", result);
        StringAssert.Contains("(long)g__Ints_Length * 4", result);
        StringAssert.Contains("Buffer.MemoryCopy(g__Bytes_Ptr", result);
    }

    [Test]
    public void GeneratedCodeUsesPointerCores()
    {
        Nsd nsd = SyntaxTests.ParseNsdContent(LexerTests.VALID_NSD);

        var compiler = new Nsd1Compiler(new CompilerOptions());
        string result = compiler.Compile(nsd, "LexerTests.ValidNsd");

        StringAssert.Contains("internal readonly unsafe int SerializeCore(byte* offset)", result);
        StringAssert.Contains("internal unsafe int UnpackCore(byte* offset, byte* end)", result);
        StringAssert.Contains("private unsafe int UnpackSpan(ReadOnlySpan<byte> buffer)", result);

        // Nested messages are written and patched in a single pass.
        StringAssert.Contains("SerializeCore(offset)", result);
        StringAssert.Contains("UnpackCore(offset, offset + g__", result);
    }
}
