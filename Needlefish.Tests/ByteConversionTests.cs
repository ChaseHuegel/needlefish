using NUnit.Framework;
using System;
using System.Buffers.Binary;

namespace Needlefish.Tests;

internal class ByteConversionTests
{
    [Test]
    public unsafe void ReverseFloatEndianness()
    {
        float a = 32.1234f;
        uint converted = *(uint*)&a;
        uint dest = 0;

        dest = BinaryPrimitives.ReverseEndianness(converted);
        dest = BinaryPrimitives.ReverseEndianness(dest);

        float result = *(float*)(&dest);

        Assert.That(result, Is.EqualTo(a));
    }

    [Test]
    public unsafe void ReverseDoubleEndianness()
    {
        double a = 32.1234d;
        ulong converted = *(ulong*)(&a);
        ulong dest = 0;

        dest = BinaryPrimitives.ReverseEndianness(converted);
        dest = BinaryPrimitives.ReverseEndianness(dest);

        double result = *(double*)(&dest);

        Assert.That(result, Is.EqualTo(a));
    }

    [Test]
    public unsafe void PointerTraversaleLength()
    {
        int length = 0;
        byte[] buffer = new byte[100];
        fixed (byte* b = &buffer[0])
        {
            byte* end = b + 100;
            byte* offset = b;
            offset += 23;

            length = (int)(offset - b);
        }

        Assert.That(length, Is.EqualTo(23));
    }

    [TestCase(5.1234f)]
    public unsafe void FloatEncoding(float value)
    {
        byte[] buffer = new byte[10];

        fixed (byte* b = &buffer[0])
        {
            byte* offset = b;

            float g__FloAT_Copy = value;

            *((uint*)offset) = BitConverter.IsLittleEndian ? *(uint*)&g__FloAT_Copy : BinaryPrimitives.ReverseEndianness(*(uint*)&g__FloAT_Copy);
            offset += 4;
        }

        fixed (byte* b = &buffer[0])
        {
            byte* offset = b;

            uint dest = BitConverter.IsLittleEndian ? *((uint*)offset) : BinaryPrimitives.ReverseEndianness(*((uint*)offset));
            offset += 4;
            float decodedValue = *(float*)&dest;

            Assert.That(decodedValue, Is.EqualTo(value));
        }
    }

    [TestCase((ushort)100)]
    public void UShort(ushort value)
    {
        byte[] buffer = new byte[2];

        buffer[0] = (byte)(value << 8);
        buffer[1] = (byte)value;

        ushort decodedValue = (ushort)((buffer[0] << 8) | buffer[1]);

        Assert.That(decodedValue, Is.EqualTo(value));
    }

    [TestCase((short)100)]
    public void Short(short value)
    {
        byte[] buffer = new byte[2];

        buffer[0] = (byte)(value << 8);
        buffer[1] = (byte)value;

        ushort decodedValue = (ushort)((buffer[0] << 8) | buffer[1]);

        Assert.That(decodedValue, Is.EqualTo(value));
    }

    [TestCase(100)]
    public void Int(int value)
    {
        byte[] buffer = new byte[4];

        buffer[0] = (byte)(value << 24);
        buffer[1] = (byte)(value << 16);
        buffer[2] = (byte)(value << 8);
        buffer[3] = (byte)value;

        int decodedValue = (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];

        Assert.That(decodedValue, Is.EqualTo(value));
    }

    [TestCase((uint)100)]
    public void Uint(uint value)
    {
        byte[] buffer = new byte[4];

        buffer[0] = (byte)(value << 24);
        buffer[1] = (byte)(value << 16);
        buffer[2] = (byte)(value << 8);
        buffer[3] = (byte)value;

        uint decodedValue = (uint)((buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);

        Assert.That(decodedValue, Is.EqualTo(value));
    }
}
