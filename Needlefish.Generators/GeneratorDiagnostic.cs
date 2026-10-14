using Microsoft.CodeAnalysis;

namespace Needlefish.Generators;

/// <summary>
/// A diagnostic raised by the generator. Wraps both core nsd diagnostics and generator
/// configuration problems so they can flow through a single equatable pipeline.
/// </summary>
internal readonly struct GeneratorDiagnostic : IEquatable<GeneratorDiagnostic>
{
    public string Id { get; }

    public string Message { get; }

    public DiagnosticSeverity Severity { get; }

    public int Position { get; }

    public int Length { get; }

    public GeneratorDiagnostic(
        string id,
        string message,
        DiagnosticSeverity severity,
        int position = -1,
        int length = 0)
    {
        Id = id;
        Message = message;
        Severity = severity;
        Position = position;
        Length = length;
    }

    public static GeneratorDiagnostic Error(string id, string message, int position = -1, int length = 0)
    {
        return new GeneratorDiagnostic(id, message, DiagnosticSeverity.Error, position, length);
    }

    public static GeneratorDiagnostic Warning(string id, string message, int position = -1, int length = 0)
    {
        return new GeneratorDiagnostic(id, message, DiagnosticSeverity.Warning, position, length);
    }

    public bool Equals(GeneratorDiagnostic other)
    {
        return string.Equals(Id, other.Id, StringComparison.Ordinal)
            && string.Equals(Message, other.Message, StringComparison.Ordinal)
            && Severity == other.Severity
            && Position == other.Position
            && Length == other.Length;
    }

    public override bool Equals(object? obj)
    {
        return obj is GeneratorDiagnostic other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = StringComparer.Ordinal.GetHashCode(Id);
            hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Message);
            hashCode = (hashCode * 397) ^ (int)Severity;
            hashCode = (hashCode * 397) ^ Position;
            hashCode = (hashCode * 397) ^ Length;
            return hashCode;
        }
    }
}
