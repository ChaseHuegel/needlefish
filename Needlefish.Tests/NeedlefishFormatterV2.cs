using System;
using System.Buffers.Binary;

namespace Needlefish;

public static class NeedlefishFormatterV2
{
    public static void WriteHeader(byte[] buffer, ref int offset, ushort id, bool isOptional = false, bool hasValue = false, bool isArray = false, ushort arrayLength = 0)
    {
        Write(buffer, ref offset, id);

        if (isOptional)
        {
            Write(buffer, ref offset, hasValue);
        }

        if (isArray && (hasValue || !isOptional))
        {
            Write(buffer, ref offset, arrayLength);
        }
    }

    public static void Write(byte[] buffer, ref int offset, bool value)
    {
        unchecked
        {
            buffer[offset] = value ? (byte)1 : (byte)0;
            offset += 1;
        }
    }

    public static bool ReadBool(byte[] buffer, ref int offset)
    {
        unchecked
        {
            bool value = buffer[offset] != 0;
            offset += 1;
            return value;
        }
    }

    public static void Write(byte[] buffer, ref int offset, short value)
    {
        unchecked
        {
            BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
            offset += 2;
        }
    }

    public static short ReadShort(byte[] buffer, ref int offset)
    {
        unchecked
        {
            offset += 2;
            return BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(offset));
        }
    }

    public static void Write(byte[] buffer, ref int offset, ushort value)
    {
        unchecked
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            offset += 2;
        }
    }

    public static ushort ReadUShort(byte[] buffer, ref int offset)
    {
        unchecked
        {
            offset += 2;
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset));
        }
    }

    public static void Write(byte[] buffer, ref int offset, int value)
    {
        unchecked
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            offset += 4;
        }
    }

    public static int ReadInt(byte[] buffer, ref int offset)
    {
        unchecked
        {
            offset += 4;
            return BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(offset));
        }
    }

    public static void Write(byte[] buffer, ref int offset, uint value)
    {
        unchecked
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            offset += 4;
        }
    }

    public static uint ReadUInt(byte[] buffer, ref int offset)
    {
        unchecked
        {
            offset += 4;
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset));
        }
    }
}
