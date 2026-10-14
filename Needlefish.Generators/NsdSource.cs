using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Needlefish.Generators;

/// <summary>
/// The fully resolved input for a single nsd file: generated source, diagnostics, and the
/// identity needed to add it to the compilation. Equality drives incremental caching, so two
/// creations from unchanged inputs compare equal and skip regeneration.
/// </summary>
internal sealed class NsdSource : IEquatable<NsdSource?>
{
    private const string PartialOptionName = "NeedlefishPartial";
    private const string PartialPropertyKey = "build_property.NeedlefishPartial";
    private const string PartialMetadataKey = "build_metadata.AdditionalFiles.NeedlefishPartial";
    private const string ProjectDirPropertyKey = "build_property.ProjectDir";
    private const string AllowUnsafeBlocksPropertyKey = "build_property.AllowUnsafeBlocks";

    private static readonly bool IsCaseInsensitiveFileSystem =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    private static readonly StringComparer PathComparer = IsCaseInsensitiveFileSystem
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private static readonly StringComparison PathComparison = IsCaseInsensitiveFileSystem
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public string HintName { get; }

    public string FilePath { get; }

    public SourceText? Text { get; }

    public string? Source { get; }

    public GeneratorDiagnostic[] Diagnostics { get; }

    private NsdSource(
        string hintName,
        string filePath,
        SourceText? text,
        string? source,
        GeneratorDiagnostic[] diagnostics)
    {
        HintName = hintName;
        FilePath = filePath;
        Text = text;
        Source = source;
        Diagnostics = diagnostics;
    }

    public static NsdSource Create(
        AdditionalText file,
        AnalyzerConfigOptionsProvider options,
        CancellationToken cancellationToken)
    {
        SourceText? text = file.GetText(cancellationToken);
        string content = text?.ToString() ?? string.Empty;

        var diagnostics = new List<GeneratorDiagnostic>();
        var compilerOptions = new CompilerOptions();

        if (TryReadBoolOption(file, options, PartialOptionName, PartialPropertyKey, PartialMetadataKey, diagnostics, out bool partial))
        {
            compilerOptions.Partial = partial;
        }

        if (options.GlobalOptions.TryGetValue(AllowUnsafeBlocksPropertyKey, out string? allowUnsafe)
            && !string.IsNullOrWhiteSpace(allowUnsafe)
            && bool.TryParse(allowUnsafe, out bool unsafeEnabled)
            && !unsafeEnabled)
        {
            diagnostics.Add(GeneratorDiagnostic.Error(
                NsdDiagnostics.UnsafeBlocksRequiredId,
                "The Needlefish generator emits unsafe serialization code. Set <AllowUnsafeBlocks>true</AllowUnsafeBlocks> in the project."));
        }

        string? relativePath = GetRelativePath(file.Path, options);

        NsdEmitResult result = new Nsd1Emitter(new EmitterOptions
        {
            CompilerOptions = compilerOptions,
        }).TryEmit(GetDisplayName(file.Path, relativePath), content);

        foreach (NsdDiagnostic diagnostic in result.Diagnostics)
        {
            diagnostics.Add(diagnostic.Level == NsdDiagnosticLevel.Error
                ? GeneratorDiagnostic.Error(NsdDiagnostics.NsdErrorId, diagnostic.Message, diagnostic.Position, diagnostic.Length)
                : GeneratorDiagnostic.Warning(NsdDiagnostics.NsdWarningId, diagnostic.Message, diagnostic.Position, diagnostic.Length));
        }

        return new NsdSource(
            GetHintName(file.Path, relativePath),
            file.Path,
            text,
            result.Source,
            diagnostics.ToArray());
    }

    public void Emit(SourceProductionContext context)
    {
        foreach (GeneratorDiagnostic diagnostic in Diagnostics)
        {
            context.ReportDiagnostic(NsdDiagnostics.ToDiagnostic(diagnostic, FilePath, Text));
        }

        if (Source != null)
        {
            context.AddSource(HintName, SourceText.From(Source, Encoding.UTF8));
        }
    }

    public bool Equals(NsdSource? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!string.Equals(HintName, other.HintName, StringComparison.Ordinal)
            || !string.Equals(FilePath, other.FilePath, StringComparison.Ordinal)
            || !string.Equals(Source, other.Source, StringComparison.Ordinal)
            || Diagnostics.Length != other.Diagnostics.Length)
        {
            return false;
        }

        for (int i = 0; i < Diagnostics.Length; i++)
        {
            if (!Diagnostics[i].Equals(other.Diagnostics[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as NsdSource);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = StringComparer.Ordinal.GetHashCode(HintName);
            hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(FilePath);
            hashCode = (hashCode * 397) ^ (Source == null ? 0 : StringComparer.Ordinal.GetHashCode(Source));

            foreach (GeneratorDiagnostic diagnostic in Diagnostics)
            {
                hashCode = (hashCode * 397) ^ diagnostic.GetHashCode();
            }

            return hashCode;
        }
    }

    private static bool TryReadBoolOption(
        AdditionalText file,
        AnalyzerConfigOptionsProvider options,
        string optionName,
        string propertyKey,
        string metadataKey,
        List<GeneratorDiagnostic> diagnostics,
        out bool value)
    {
        value = false;

        if (options.GetOptions(file).TryGetValue(metadataKey, out string? metadataValue)
            && !string.IsNullOrWhiteSpace(metadataValue))
        {
            if (bool.TryParse(metadataValue, out value))
            {
                return true;
            }

            diagnostics.Add(GeneratorDiagnostic.Warning(
                NsdDiagnostics.InvalidOptionId,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Invalid value '{0}' for per-file option '{1}'. Expected 'true' or 'false'.",
                    metadataValue,
                    optionName)));
            return false;
        }

        if (options.GlobalOptions.TryGetValue(propertyKey, out string? propertyValue)
            && !string.IsNullOrWhiteSpace(propertyValue))
        {
            if (bool.TryParse(propertyValue, out value))
            {
                return true;
            }

            diagnostics.Add(GeneratorDiagnostic.Warning(
                NsdDiagnostics.InvalidOptionId,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Invalid value '{0}' for MSBuild property '{1}'. Expected 'true' or 'false'.",
                    propertyValue,
                    optionName)));
            return false;
        }

        return true;
    }

    private static string GetHintName(string path, string? relativePath)
    {
        if (relativePath != null)
        {
            return Sanitize(Path.ChangeExtension(relativePath, null) ?? relativePath) + ".g.cs";
        }

        return Sanitize(Path.GetFileNameWithoutExtension(path)) + "_" + PathHash(path) + ".g.cs";
    }

    private static string GetDisplayName(string path, string? relativePath)
    {
        if (relativePath != null)
        {
            return Path.ChangeExtension(relativePath, null) ?? relativePath;
        }

        return Path.GetFileNameWithoutExtension(path);
    }

    private static string? GetRelativePath(string path, AnalyzerConfigOptionsProvider options)
    {
        if (!options.GlobalOptions.TryGetValue(ProjectDirPropertyKey, out string? projectDir)
            || string.IsNullOrWhiteSpace(projectDir))
        {
            return null;
        }

        try
        {
            string baseFullPath = Path.GetFullPath(projectDir);
            if (!baseFullPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                baseFullPath += Path.DirectorySeparatorChar;
            }

            string fileFullPath = Path.GetFullPath(path);
            if (!fileFullPath.StartsWith(baseFullPath, PathComparison))
            {
                return null;
            }

            string relativePath = fileFullPath.Substring(baseFullPath.Length);
            return string.IsNullOrWhiteSpace(relativePath) ? null : relativePath;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (char character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) || character == '.' ? character : '_');
        }

        return builder.ToString();
    }

    private static string PathHash(string path)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        uint hash = offsetBasis;
        byte[] bytes = Encoding.UTF8.GetBytes(path);

        foreach (byte value in bytes)
        {
            hash ^= value;
            hash *= prime;
        }

        return hash.ToString("x8", CultureInfo.InvariantCulture);
    }
}
