namespace Needlefish;

/// <summary>
/// The severity of a diagnostic produced while compiling an nsd file.
/// </summary>
public enum NsdDiagnosticLevel
{
    Warning,
    Error,
}

/// <summary>
/// Describes a problem encountered while compiling an nsd file. Positions are 0-based
/// character offsets into the source text, or <c>-1</c> when a precise location is unknown.
/// </summary>
public readonly struct NsdDiagnostic
{
    public string Message { get; }

    public NsdDiagnosticLevel Level { get; }

    public int Position { get; }

    public int Length { get; }

    /// <summary>
    /// The file the problem was found in, or <c>null</c> when it belongs to the file being
    /// compiled.
    /// </summary>
    public string? FilePath { get; }

    public NsdDiagnostic(
        string message,
        NsdDiagnosticLevel level = NsdDiagnosticLevel.Error,
        int position = -1,
        int length = 0,
        string? filePath = null)
    {
        Message = message;
        Level = level;
        Position = position;
        Length = length;
        FilePath = filePath;
    }
}
