using NUnit.Framework;

namespace Needlefish.Tests;

internal class GeneratedCodeTests
{
    [Test]
    public void GetSize()
    {
        var message = new Lexer.Tests.TestMessage
        {
            Content = "quick",
            Body = "brown",
            FloAT = 1.234f,
            Double = 5.678d,
            Long = 1337,
            uLong = 1338,
            Ulong = 1339,
            Short = 1340,
            UShort = 1341,
            Bool = true,
            Byte = 128,
            Bytes = new byte[] { 1, 5, 12, 39 },
            OptionalStrings = new string[] { "fox", "jumped", "over", "the fence" },
            Int = 1342,
            OptionalInt = 1343,
            Ints = new int[] { 1344, 1345, 1355 },
            OptionalInts = new int[] { 1346, 1347 },
            UInt = 1348,
            OptionalUInt = 1349,
            UInts = new uint[] { 1350, 1351 },
            OptionalUInts = new uint[] { 1352, 1353 },
            Enum = Lexer.Tests.TestEnum.Val2,
            OptionalEnum = Lexer.Tests.TestEnum.Val3,
            Enums = new Lexer.Tests.TestEnum[] { Lexer.Tests.TestEnum.Val3, Lexer.Tests.TestEnum.Val2, Lexer.Tests.TestEnum.Val1 },
            OptionalEnums = new Lexer.Tests.TestEnum[] { Lexer.Tests.TestEnum.Val3, Lexer.Tests.TestEnum.Val2, Lexer.Tests.TestEnum.Val1 },
            Submessage = new Lexer.Tests.Submessage { OptionalInt = 1 },
            OptionalSubmessage = new Lexer.Tests.Submessage { OptionalInt = 2 },
            Submessages = new Lexer.Tests.Submessage[] { new Lexer.Tests.Submessage { OptionalInt = 3 }, new Lexer.Tests.Submessage { OptionalInt = 4 } },
            OptionalSubmessages = new Lexer.Tests.Submessage[] { new Lexer.Tests.Submessage { OptionalInt = 5 }, new Lexer.Tests.Submessage { OptionalInt = 6 } },
        };

        Assert.That(message.GetSize(), Is.EqualTo(348));
    }
}
