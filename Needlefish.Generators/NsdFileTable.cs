using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Needlefish.Schema;

namespace Needlefish.Generators;

/// <summary>
/// The project's additional files keyed by canonical path. Serves as the include resolver for the
/// generator and provides the source texts diagnostics are reported against.
/// </summary>
internal sealed class NsdFileTable : INsdFileResolver, IEquatable<NsdFileTable?>
{
    private readonly ImmutableDictionary<string, SourceText> _texts;

    private NsdFileTable(ImmutableDictionary<string, SourceText> texts, string[] rootPaths)
    {
        _texts = texts;
        RootPaths = rootPaths;
    }

    /// <summary>
    /// The canonical paths of the files that are compiled as roots, meaning files with the nsd
    /// extension.
    /// </summary>
    public string[] RootPaths { get; }

    public static NsdFileTable Create(ImmutableArray<AdditionalText> files, CancellationToken cancellationToken)
    {
        ImmutableDictionary<string, SourceText>.Builder texts =
            ImmutableDictionary.CreateBuilder<string, SourceText>(NsdPaths.Comparer);

        foreach (AdditionalText file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!file.Path.EndsWith(NsdGenerator.NsdExtension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string path = NsdPaths.Normalize(file.Path);
            if (texts.ContainsKey(path))
            {
                continue;
            }

            SourceText? text = file.GetText(cancellationToken);
            if (text != null)
            {
                texts[path] = text;
            }
        }

        ImmutableDictionary<string, SourceText> built = texts.ToImmutable();

        string[] rootPaths = built.Keys
            .OrderBy(path => path, NsdPaths.Comparer)
            .ToArray();

        return new NsdFileTable(built, rootPaths);
    }

    public NsdFileResolution Resolve(string includingFilePath, string includePath)
    {
        string resolvedPath = NsdPaths.ResolveIncludePath(includingFilePath, includePath);

        if (_texts.TryGetValue(resolvedPath, out SourceText? text))
        {
            return NsdFileResolution.Resolved(resolvedPath, text.ToString());
        }

        return NsdFileResolution.Failed(
            resolvedPath,
            string.Format(
                "Included file '{0}' was not found. Make sure the file is part of the project.",
                includePath));
    }

    public SourceText? GetSourceText(string path)
    {
        return _texts.TryGetValue(path, out SourceText? text) ? text : null;
    }

    public bool Equals(NsdFileTable? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (_texts.Count != other._texts.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, SourceText> pair in _texts)
        {
            if (!other._texts.TryGetValue(pair.Key, out SourceText? otherText)
                || !pair.Value.ContentEquals(otherText))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as NsdFileTable);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = _texts.Count;

            foreach (string path in _texts.Keys)
            {
                hashCode = (hashCode * 397) ^ NsdPaths.Comparer.GetHashCode(path);
            }

            return hashCode;
        }
    }
}
