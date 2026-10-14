using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Needlefish.Generators;

internal static class NsdDiagnostics
{
    public const string InvalidOptionId = "NSD0001";
    public const string UnsafeBlocksRequiredId = "NSD0002";
    public const string NsdErrorId = "NSD1001";
    public const string NsdWarningId = "NSD1002";

    private const string Category = "Needlefish";

    private static readonly DiagnosticDescriptor InvalidOption = new(
        id: InvalidOptionId,
        title: "Invalid Needlefish option",
        messageFormat: "{0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsafeBlocksRequired = new(
        id: UnsafeBlocksRequiredId,
        title: "Unsafe code is required",
        messageFormat: "{0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NsdError = new(
        id: NsdErrorId,
        title: "Invalid nsd",
        messageFormat: "{0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NsdWarning = new(
        id: NsdWarningId,
        title: "nsd warning",
        messageFormat: "{0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static Diagnostic ToDiagnostic(GeneratorDiagnostic diagnostic, string path, SourceText? text)
    {
        DiagnosticDescriptor descriptor = diagnostic.Id switch
        {
            InvalidOptionId => InvalidOption,
            UnsafeBlocksRequiredId => UnsafeBlocksRequired,
            NsdWarningId => NsdWarning,
            _ => NsdError,
        };

        return Diagnostic.Create(descriptor, CreateLocation(path, text, diagnostic.Position, diagnostic.Length), diagnostic.Message);
    }

    private static Location CreateLocation(string path, SourceText? text, int position, int length)
    {
        if (position < 0 || text == null)
        {
            return Location.Create(
                path,
                new TextSpan(0, 0),
                new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));
        }

        int start = Math.Min(position, text.Length);
        int clampedLength = Math.Max(0, Math.Min(length, text.Length - start));
        var span = new TextSpan(start, clampedLength);

        return Location.Create(path, span, text.Lines.GetLinePositionSpan(span));
    }
}
