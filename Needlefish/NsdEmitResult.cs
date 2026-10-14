using System;

namespace Needlefish;

/// <summary>
/// The result of an <see cref="Nsd1Emitter.TryEmit(string, string)"/> call. <see cref="Source"/> is
/// <c>null</c> when the nsd could not be compiled.
/// </summary>
public readonly struct NsdEmitResult
{
    public string? Source { get; }

    public NsdDiagnostic[] Diagnostics { get; }

    public bool Success => Source != null;

    public NsdEmitResult(string? source, NsdDiagnostic[] diagnostics)
    {
        Source = source;
        Diagnostics = diagnostics ?? Array.Empty<NsdDiagnostic>();
    }
}
