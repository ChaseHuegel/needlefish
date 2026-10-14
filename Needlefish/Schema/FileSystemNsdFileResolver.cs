using System;
using System.IO;
using System.Security;

namespace Needlefish.Schema;

/// <summary>
/// Resolves include directives against the local file system, relative to the including document.
/// </summary>
public sealed class FileSystemNsdFileResolver : INsdFileResolver
{
    public NsdFileResolution Resolve(string includingFilePath, string includePath)
    {
        string resolvedPath = NsdPaths.ResolveIncludePath(includingFilePath, includePath);

        try
        {
            if (!File.Exists(resolvedPath))
            {
                return NsdFileResolution.Failed(
                    resolvedPath,
                    string.Format("Included file '{0}' was not found (looked for '{1}').", includePath, resolvedPath));
            }

            return NsdFileResolution.Resolved(resolvedPath, File.ReadAllText(resolvedPath));
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ArgumentException
            or SecurityException)
        {
            return NsdFileResolution.Failed(
                resolvedPath,
                string.Format("Included file '{0}' could not be read: {1}", includePath, exception.Message));
        }
    }
}
