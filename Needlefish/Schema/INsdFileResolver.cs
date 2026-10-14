namespace Needlefish.Schema;

/// <summary>
/// Resolves the path of an include directive to the content of the included nsd document.
/// Implementations decide where documents are loaded from, such as the file system or the
/// source generator's additional files.
/// </summary>
public interface INsdFileResolver
{
    /// <summary>
    /// Resolves <paramref name="includePath"/> as written in <paramref name="includingFilePath"/>.
    /// </summary>
    NsdFileResolution Resolve(string includingFilePath, string includePath);
}
