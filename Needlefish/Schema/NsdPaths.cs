using System;
using System.IO;

namespace Needlefish.Schema;

/// <summary>
/// Path helpers shared by include resolution implementations.
/// </summary>
public static class NsdPaths
{
    private static readonly bool IsCaseInsensitiveFileSystem =
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
        || System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);

    /// <summary>
    /// Compares paths using the case sensitivity of the current file system.
    /// </summary>
    public static StringComparer Comparer { get; } = IsCaseInsensitiveFileSystem
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    /// <summary>
    /// Compares paths using the case sensitivity of the current file system.
    /// </summary>
    public static StringComparison Comparison { get; } = IsCaseInsensitiveFileSystem
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    /// <summary>
    /// Returns the full path for <paramref name="path"/>, or the original value when it cannot be
    /// normalized.
    /// </summary>
    public static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path;
        }
    }

    /// <summary>
    /// Resolves <paramref name="includePath"/> relative to the directory of
    /// <paramref name="includingFilePath"/>.
    /// </summary>
    public static string ResolveIncludePath(string includingFilePath, string includePath)
    {
        string directory = Path.GetDirectoryName(Normalize(includingFilePath)) ?? string.Empty;
        return Normalize(Path.Combine(directory, includePath));
    }
}
