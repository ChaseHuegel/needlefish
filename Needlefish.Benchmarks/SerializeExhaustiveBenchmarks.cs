using BenchmarkDotNet.Attributes;
using Needlefish.Benchmarks.Models;

namespace Needlefish.Benchmarks;

/// <summary>
/// Serialization of the exhaustive model: all primitives, optional fields, arrays,
/// enums and nested messages. <see cref="StringLength"/> drives the two string fields.
/// </summary>
[MemoryDiagnoser]
public class SerializeExhaustiveBenchmarks
{
    private Exhaustive _message;
    private byte[] _buffer = null!;

    [Params(16, 1024)]
    public int StringLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _message = PayloadFactory.CreateExhaustive(StringLength);
        _buffer = new byte[_message.GetSize()];
    }

    [Benchmark]
    public int GetSize() => _message.GetSize();

    [Benchmark]
    public int SerializeInto() => _message.SerializeInto(_buffer, 0);

    [Benchmark]
    public byte[] Serialize() => _message.Serialize();
}
