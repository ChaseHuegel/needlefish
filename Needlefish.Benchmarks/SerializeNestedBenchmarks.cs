using BenchmarkDotNet.Attributes;
using Needlefish.Benchmarks.Models;

namespace Needlefish.Benchmarks;

/// <summary>
/// Serialization of nested message arrays (AddressBook -> Person -> Phone[]).
/// This is the scenario where the current generator recomputes nested sizes once per
/// ancestor level.
/// </summary>
[MemoryDiagnoser]
public class SerializeNestedBenchmarks
{
    private AddressBook _book;
    private byte[] _buffer = null!;

    [Params(16, 128)]
    public int People { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _book = PayloadFactory.CreateAddressBook(People, phonesPerPerson: 4);
        _buffer = new byte[_book.GetSize()];
    }

    [Benchmark]
    public int GetSize() => _book.GetSize();

    [Benchmark]
    public int SerializeInto() => _book.SerializeInto(_buffer, 0);

    [Benchmark]
    public byte[] Serialize() => _book.Serialize();
}
