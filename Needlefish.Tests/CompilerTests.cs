using Needlefish.Compile;
using Needlefish.Schema;
using NUnit.Framework;

namespace Needlefish.Tests;

internal class CompilerTests
{
    [Test]
    public void Compile()
    {
        Nsd nsd = ParserTests.ParseNsdContent(LexerTests.ValidNsd);

        var compiler = new Nsd1Compiler();

        string result = compiler.Compile(nsd, "LexerTests.ValidNsd");

        Assert.Pass(result);
    }
}
