using BenchmarkDotNet.Attributes;
using Needlefish.Benchmarks.Models;
using System;

namespace Needlefish.Benchmarks;

/// <summary>
/// Deserialization of the exhaustive model. The read path allocates the strings and
/// arrays the model requires; these numbers are the baseline for the codegen work.
/// </summary>
[MemoryDiagnoser]
public class DeserializeExhaustiveBenchmarks
{
    private byte[] _buffer = null!;
    private Exhaustive _target;

    [Params(16, 1024)]
    public int StringLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Exhaustive source = PayloadFactory.CreateExhaustive(StringLength);
        _buffer = source.Serialize();
    }

    [Benchmark]
    public int Unpack_Fresh()
    {
        Exhaustive target = new();
        return target.Unpack(_buffer, 0, _buffer.Length);
    }

    [Benchmark]
    public int Unpack_Reused() => _target.Unpack(_buffer, 0, _buffer.Length);

    [Benchmark]
    public Exhaustive Deserialize() => Exhaustive.Deserialize(_buffer.AsSpan());
}
