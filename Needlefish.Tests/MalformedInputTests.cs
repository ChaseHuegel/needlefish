using Lexer.Tests;
using NUnit.Framework;
using System;
using System.IO;

namespace Needlefish.Tests;

internal class MalformedInputTests
{
    // Field identifiers from LexerTests.VALID_NSD.
    private const byte Int_ID = 31;
    private const byte OptionalInt_ID = 32;
    private const byte Body_ID = 1;
    private const byte Bytes_ID = 21;
    private const byte OptionalStrings_ID = 30;
    private const byte Submessage_ID = 26;

    private static byte[] Buffer(params byte[] bytes) => bytes;

    [Test]
    public void IntValueHeaderOnly()
    {
        // Header only: 2 bytes. The loop bound must still surface the truncation.
        Assert.Throws<InvalidDataException>(() =>
        {
            TestMessage message = new();
            message.Unpack(Buffer(Int_ID, 0), 0, 2);
        });
    }

    [Test]
    public void IntValueMissingOneByte()
    {
        Assert.Throws<InvalidDataException>(() =>
        {
            TestMessage message = new();
            message.Unpack(Buffer(Int_ID, 0, 0, 0, 0), 0, 5);
        });
    }

    [Test]
    public void IntValueMissingTwoBytes()
    {
        Assert.Throws<InvalidDataException>(() =>
        {
            TestMessage message = new();
            message.Unpack(Buffer(Int_ID, 0, 0, 0), 0, 4);
        });
    }

    [Test]
    public void IntValueMissingThreeBytes()
    {
        Assert.Throws<InvalidDataException>(() =>
        {
            TestMessage message = new();
            message.Unpack(Buffer(Int_ID, 0, 0), 0, 3);
        });
    }

    [Test]
    public void IntValueComplete()
    {
        TestMessage message = new();
        int read = message.Unpack(Buffer(Int_ID, 0, 5, 0, 0, 0), 0, 6);

        Assert.That(read, Is.EqualTo(6));
        Assert.That(message.Int, Is.EqualTo(5));
    }

    [Test]
    public void OptionalIntValueMissingValue()
    {
        // Header + HasValue flag (1) but no 4-byte value.
        Assert.Throws<InvalidDataException>(() =>
        {
            TestMessage message = new();
            message.Unpack(Buffer(OptionalInt_ID, 0, 1), 0, 3);
        });
    }

    [Test]
    public void OptionalIntValuePartial()
    {
        // Header + flag byte (1) + only 2 of 4 value bytes.
        Assert.Throws<InvalidDataException>(() =>
        {
            TestMessage message = new();
            message.Unpack(Buffer(OptionalInt_ID, 0, 1, 0, 0), 0, 5);
        });
    }

    [Test]
    public void OptionalIntAbsent()
    {
        // HasValue = 0 requires no value bytes.
        TestMessage message = new();
        int read = message.Unpack(Buffer(OptionalInt_ID, 0, 0), 0, 3);

        Assert.That(read, Is.EqualTo(3));
        Assert.That(message.OptionalInt, Is.Null);
    }

    [Test]
    public void StringLengthHeaderMissing()
    {
        // Header only, no 2-byte length.
        Assert.Throws<InvalidDataException>(() =>
        {
            TestMessage message = new();
            message.Unpack(Buffer(Body_ID, 0), 0, 2);
        });
    }

    [Test]
    public void StringCharsTruncated()
    {
        // Length 3 but only 1 of 3 chars (2 of 6 bytes) present.
        Assert.Throws<InvalidDataException>(() =>
        {
            TestMessage message = new();
            message.Unpack(Buffer(Body_ID, 0, 3, 0, (byte)'a', 0), 0, 6);
        });
    }

    [Test]
    public void ArrayHeaderMissing()
    {
        // Header only, no 2-byte length.
        Assert.Throws<InvalidDataException>(() =>
        {
            TestMessage message = new();
            message.Unpack(Buffer(Bytes_ID, 0), 0, 2);
        });
    }

    [Test]
    public void ArrayElementsTruncated()
    {
        // Length 3 but only 2 element bytes present.
        Assert.Throws<InvalidDataException>(() =>
        {
            TestMessage message = new();
            message.Unpack(Buffer(Bytes_ID, 0, 3, 0, 1, 2), 0, 6);
        });
    }

    [Test]
    public void StringArrayElementTruncated()
    {
        // OptionalStrings: header + HasValue(1) + count(1) + element length(3) + 1 char.
        Assert.Throws<InvalidDataException>(() =>
        {
            TestMessage message = new();
            message.Unpack(Buffer(OptionalStrings_ID, 0, 1, 1, 0, 3, 0, (byte)'a', 0), 0, 9);
        });
    }

    [Test]
    public void SubmessageLengthExceedsBuffer()
    {
        // ObjectLength claims 10 bytes but only 6 are present.
        Assert.Throws<InvalidDataException>(() =>
        {
            TestMessage message = new();
            message.Unpack(Buffer(Submessage_ID, 0, 10, 0, 0, 0, 0, 0), 0, 8);
        });
    }

    [Test]
    public void SubmessageContentTruncated()
    {
        // ObjectLength 2, whose content is a lone field header with no value.
        // Submessage.OptionalInt has field ID 0 (its first field).
        Assert.Throws<InvalidDataException>(() =>
        {
            TestMessage message = new();
            message.Unpack(Buffer(Submessage_ID, 0, 2, 0, 0, 0), 0, 6);
        });
    }

    [Test]
    public void StartAndLengthExceedBuffer()
    {
        TestMessage message = new();
        byte[] buffer = Buffer(Int_ID, 0, 5, 0, 0, 0);

        Assert.Throws<InvalidDataException>(() => message.Unpack(buffer, 0, buffer.Length + 1));
    }

    [Test]
    public void NegativeLength()
    {
        TestMessage message = new();
        byte[] buffer = Buffer(Int_ID, 0);

        Assert.Throws<InvalidDataException>(() => message.Unpack(buffer, 0, -1));
    }

    [Test]
    public void DeserializeRejectsTruncatedInput()
    {
        Assert.Throws<InvalidDataException>(() => TestMessage.Deserialize(Buffer(Int_ID, 0, 0, 0, 0), 0, 5));
    }

    [Test]
    public void EmptyBufferReturnsZero()
    {
        TestMessage message = new();

        Assert.That(message.Unpack(Array.Empty<byte>(), 0, 0), Is.EqualTo(0));
    }

    [Test]
    public void RoundTrip()
    {
        var original = new TestMessage
        {
            Content = "hello",
            Body = "world",
            Int = 42,
            OptionalInt = 7,
            Bytes = new byte[] { 1, 2, 3 },
            Ints = new int[] { 4, 5 },
            OptionalStrings = new string[] { "x", "yz" },
            Enum = TestEnum.Val2,
            Enums = new TestEnum[] { TestEnum.Val1, TestEnum.Val3 },
            Submessage = new Submessage { OptionalInt = 9 },
        };

        byte[] buffer = original.Serialize();
        var decoded = new TestMessage();

        Assert.That(decoded.Unpack(buffer, 0, buffer.Length), Is.EqualTo(buffer.Length));
        Assert.That(decoded.Content, Is.EqualTo(original.Content));
        Assert.That(decoded.Body, Is.EqualTo(original.Body));
        Assert.That(decoded.Int, Is.EqualTo(original.Int));
        Assert.That(decoded.OptionalInt, Is.EqualTo(original.OptionalInt));
        Assert.That(decoded.Bytes, Is.EqualTo(original.Bytes));
        Assert.That(decoded.Ints, Is.EqualTo(original.Ints));
        Assert.That(decoded.OptionalStrings, Is.EqualTo(original.OptionalStrings));
        Assert.That(decoded.Enum, Is.EqualTo(original.Enum));
        Assert.That(decoded.Enums, Is.EqualTo(original.Enums));
        Assert.That(decoded.Submessage.OptionalInt, Is.EqualTo(original.Submessage.OptionalInt));
    }
}
