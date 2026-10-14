namespace Needlefish.Schema;

/// <summary>
/// The outcome of resolving an include directive. <see cref="Success"/> is <c>false</c> when the
/// included document does not exist or cannot be read; <see cref="Error"/> then describes why.
/// </summary>
public readonly struct NsdFileResolution
{
    /// <summary>
    /// The canonical path of the included document. Set even when resolution failed so callers can
    /// report which path was looked for.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The content of the included document, or <c>null</c> when resolution failed.
    /// </summary>
    public string? Content { get; }

    /// <summary>
    /// A description of why resolution failed, or <c>null</c> on success.
    /// </summary>
    public string? Error { get; }

    public bool Success => Content != null;

    private NsdFileResolution(string path, string? content, string? error)
    {
        Path = path;
        Content = content;
        Error = error;
    }

    public static NsdFileResolution Resolved(string path, string content)
    {
        return new NsdFileResolution(path, content, null);
    }

    public static NsdFileResolution Failed(string path, string error)
    {
        return new NsdFileResolution(path, null, error);
    }
}
