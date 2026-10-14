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
}
