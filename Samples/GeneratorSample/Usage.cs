using System;

namespace Samples.GeneratorSample;

public static class Usage
{
    public static int RoundTrip()
    {
        var book = new AddressBook
        {
            People = new[]
            {
                new Person { Id = 1, Name = "Ada" },
            },
        };

        byte[] buffer = book.Serialize();
        AddressBook decoded = AddressBook.Deserialize(buffer.AsSpan());

        var point = new Point { X = 3, Y = 4 };

        return decoded.People.Length + point.X + point.Y;
    }
}
