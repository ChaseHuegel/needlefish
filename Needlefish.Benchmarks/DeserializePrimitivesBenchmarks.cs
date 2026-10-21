using BenchmarkDotNet.Attributes;
using Needlefish.Benchmarks.Models;
using System;

namespace Needlefish.Benchmarks;

/// <summary>
/// Deserialization of a message without strings or arrays. This path must not allocate
/// at all; it validates that reading itself is allocation free.
/// </summary>
[MemoryDiagnoser]
public class DeserializePrimitivesBenchmarks
{
    private byte[] _buffer = null!;
    private Primitives _target;

    [GlobalSetup]
    public void Setup()
    {
        Primitives source = PayloadFactory.CreatePrimitives();
        _buffer = source.Serialize();
    }

    [Benchmark]
    public int Unpack() => _target.Unpack(_buffer, 0, _buffer.Length);

    [Benchmark]
    public Primitives Deserialize() => Primitives.Deserialize(_buffer.AsSpan());
}
