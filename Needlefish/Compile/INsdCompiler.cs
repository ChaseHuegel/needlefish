using Needlefish.Schema;

namespace Needlefish.Compile;

internal interface INsdCompiler
{
    string Version { get; }

    string Compile(Nsd nsd, string? sourceName = null);
}