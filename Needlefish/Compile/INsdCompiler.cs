using Needlefish.Schema;

namespace Needlefish.Compile;

internal interface INsdCompiler
{
    float Version { get; }

    string Compile(Nsd nsd, string? sourceName = null);
}