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
}
