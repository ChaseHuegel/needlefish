using Needlefish.Compile;
using Needlefish.Schema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Needlefish;

public class Nsd1Emitter
{
    public struct Source(string name, string content)
    {
        public string Name = name;
        public string Content = content;
    }

    private readonly object _lock = new();
    private readonly INsdCompiler _compiler;

    public Nsd1Emitter(EmitterOptions options)
    {
        _compiler = new Nsd1Compiler(options.CompilerOptions ?? new CompilerOptions());
    }

    /// <summary>
    /// Compiles an nsd source that is not associated with a file on disk. Include directives
    /// cannot be resolved and are reported as errors.
    /// </summary>
    public string Emit(string name, string source)
    {
        lock (_lock)
        {
            NsdEmitResult result = TryEmitInternal(name, source, resolver: null, knownRoots: null, outputName: name);

            if (result.Source == null)
            {
                NsdDiagnostic diagnostic = result.Diagnostics.FirstOrDefault(d => d.Level == NsdDiagnosticLevel.Error);
                throw new NsdException(diagnostic.Message, diagnostic.Position, diagnostic.Length);
            }

            return result.Source;
        }
    }

    /// <summary>
    /// Compiles an nsd source located at <paramref name="filePath"/>, resolving include
    /// directives with <paramref name="resolver"/>. <paramref name="knownRoots"/> lists the files
    /// that are compiled as roots themselves so diagnostics from included files can be reported
    /// once.
    /// </summary>
    public string Emit(
        string filePath,
        string source,
        INsdFileResolver? resolver,
        IReadOnlyCollection<string>? knownRoots = null,
        string? outputName = null)
    {
        lock (_lock)
        {
            NsdEmitResult result = TryEmitInternal(filePath, source, resolver, knownRoots, outputName);

            if (result.Source == null)
            {
                NsdDiagnostic diagnostic = result.Diagnostics.FirstOrDefault(d => d.Level == NsdDiagnosticLevel.Error);
                throw new NsdException(diagnostic.Message, diagnostic.Position, diagnostic.Length);
            }

            return result.Source;
        }
    }

    /// <summary>
    /// Compiles an nsd source that is not associated with a file on disk, returning diagnostics
    /// instead of throwing when the source is invalid. Include directives cannot be resolved and
    /// are reported as errors.
    /// </summary>
    public NsdEmitResult TryEmit(string name, string source)
    {
        lock (_lock)
        {
            return TryEmitInternal(name, source, resolver: null, knownRoots: null, outputName: name);
        }
    }

    /// <summary>
    /// Compiles an nsd source located at <paramref name="filePath"/>, resolving include
    /// directives with <paramref name="resolver"/>, and returns diagnostics instead of throwing
    /// when the source is invalid.
    /// </summary>
    public NsdEmitResult TryEmit(
        string filePath,
        string source,
        INsdFileResolver? resolver,
        IReadOnlyCollection<string>? knownRoots = null,
        string? outputName = null)
    {
        lock (_lock)
        {
            return TryEmitInternal(filePath, source, resolver, knownRoots, outputName);
        }
    }

    public string[] Emit(Source[] sources)
    {
        string[] results = new string[sources.Length];

        lock (_lock)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                Source source = sources[i];
                NsdEmitResult result = TryEmitInternal(source.Name, source.Content, resolver: null, knownRoots: null, outputName: source.Name);

                if (result.Source == null)
                {
                    NsdDiagnostic diagnostic = result.Diagnostics.FirstOrDefault(d => d.Level == NsdDiagnosticLevel.Error);
                    throw new NsdException(diagnostic.Message, diagnostic.Position, diagnostic.Length);
                }

                results[i] = result.Source;
            }
        }

        return results;
    }

    private NsdEmitResult TryEmitInternal(
        string filePath,
        string source,
        INsdFileResolver? resolver,
        IReadOnlyCollection<string>? knownRoots,
        string? outputName)
    {
        var diagnostics = new List<NsdDiagnostic>();

        try
        {
            NsdIncludeCompiler.NsdIncludeResult result = NsdIncludeCompiler.Prepare(filePath, source, resolver, knownRoots);
            diagnostics.AddRange(result.Diagnostics);

            if (result.Root == null)
            {
                return new NsdEmitResult(null, diagnostics.ToArray());
            }

            return new NsdEmitResult(_compiler.Compile(result.Root.Value, outputName ?? filePath), diagnostics.ToArray());
        }
        catch (FormatException formatException)
        {
            diagnostics.Add(new NsdDiagnostic(formatException.Message));
        }

        return new NsdEmitResult(null, diagnostics.ToArray());
    }
}
