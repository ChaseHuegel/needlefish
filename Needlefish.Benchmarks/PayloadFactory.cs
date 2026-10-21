using Needlefish.Benchmarks.Models;

namespace Needlefish.Benchmarks;

/// <summary>
/// Deterministic payloads shared by every benchmark so runs are comparable over time.
/// </summary>
internal static class PayloadFactory
{
    public static Primitives CreatePrimitives()
    {
        return new Primitives
        {
            IntValue = 1342,
            LongValue = 1337,
            DoubleValue = 5.678d,
            FloatValue = 1.234f,
            BoolValue = true,
            ByteValue = 128,
            ShortValue = 1340,
            UShortValue = 1341,
            UIntValue = 1348,
            ULongValue = 1339,
        };
    }

    public static Exhaustive CreateExhaustive(int stringLength)
    {
        return new Exhaustive
        {
            Content = new string('c', stringLength),
            Body = new string('b', stringLength),
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
            Enum = BenchEnum.Val2,
            OptionalEnum = BenchEnum.Val3,
            Enums = new BenchEnum[] { BenchEnum.Val3, BenchEnum.Val2, BenchEnum.Val1 },
            OptionalEnums = new BenchEnum[] { BenchEnum.Val3, BenchEnum.Val2, BenchEnum.Val1 },
            Submessage = new Submessage { OptionalInt = 1 },
            OptionalSubmessage = new Submessage { OptionalInt = 2 },
            Submessages = new Submessage[] { new Submessage { OptionalInt = 3 }, new Submessage { OptionalInt = 4 } },
            OptionalSubmessages = new Submessage[] { new Submessage { OptionalInt = 5 }, new Submessage { OptionalInt = 6 } },
        };
    }

    public static AddressBook CreateAddressBook(int people, int phonesPerPerson)
    {
        AddressBook book = new()
        {
            People = new Person[people],
        };

        for (int i = 0; i < people; i++)
        {
            Phone[] phones = new Phone[phonesPerPerson];
            for (int p = 0; p < phonesPerPerson; p++)
            {
                phones[p] = new Phone
                {
                    Number = "555-" + i.ToString("D4") + "-" + p.ToString("D2"),
                    Type = (PhoneType)((i + p) % 4),
                };
            }

            book.People[i] = new Person
            {
                Id = i,
                Name = "Person " + i,
                Email = "person" + i + "@example.com",
                Phones = phones,
            };
        }

        return book;
    }
}
