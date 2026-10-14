using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Needlefish.Generators;

/// <summary>
/// Incrementally compiles .nsd files included as <c>AdditionalFiles</c> into C# types with
/// serialization support.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class NsdGenerator : IIncrementalGenerator
{
    internal const string NsdExtension = ".nsd";

    private static readonly StringComparer FilePathComparer =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<AdditionalText>> files = context.AdditionalTextsProvider.Collect();

        IncrementalValueProvider<NsdFileTable> fileTable = files
            .Select(static (texts, cancellationToken) => NsdFileTable.Create(texts, cancellationToken))
            .WithTrackingName("NsdFileTable");

        IncrementalValuesProvider<NsdSource> sources = files
            .SelectMany(static (texts, cancellationToken) => SelectNsdFiles(texts, cancellationToken))
            .Combine(fileTable)
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, cancellationToken) => NsdSource.Create(pair.Left.Left, pair.Left.Right, pair.Right, cancellationToken))
            .WithTrackingName("NsdSource");

        context.RegisterSourceOutput(sources, static (productionContext, source) => source.Emit(productionContext));
    }

    private static IEnumerable<AdditionalText> SelectNsdFiles(
        ImmutableArray<AdditionalText> files,
        CancellationToken cancellationToken)
    {
        // The same file can appear in AdditionalFiles more than once (for example when it is
        // both picked up by the default glob and listed explicitly), so deduplicate by path.
        var seenPaths = new HashSet<string>(FilePathComparer);

        foreach (AdditionalText file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!file.Path.EndsWith(NsdExtension, StringComparison.OrdinalIgnoreCase) || !seenPaths.Add(file.Path))
            {
                continue;
            }

            yield return file;
        }
    }
}
