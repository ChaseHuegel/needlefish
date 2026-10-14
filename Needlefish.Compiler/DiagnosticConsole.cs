using Needlefish;

namespace Needlefish.Compiler;

/// <summary>
/// Writes nsd diagnostics to the console with file and position information, reading file
/// contents on demand for diagnostics that were produced for included files.
/// </summary>
internal sealed class DiagnosticConsole
{
    private readonly Dictionary<string, string?> _contents = new(StringComparer.Ordinal);

    public void Register(string path, string content)
    {
        _contents[path] = content;
    }

    public void Report(NsdDiagnostic diagnostic)
    {
        string path = string.IsNullOrEmpty(diagnostic.FilePath) ? "<nsd>" : diagnostic.FilePath!;
        string severity = diagnostic.Level == NsdDiagnosticLevel.Error ? "error" : "warning";

        if (diagnostic.Position < 0 || !TryGetLineColumn(path, diagnostic.Position, out int line, out int column))
        {
            Console.Error.WriteLine($"{FormatPath(path)}: {severity}: {diagnostic.Message}");
            return;
        }

        Console.Error.WriteLine($"{FormatPath(path)}({line},{column}): {severity}: {diagnostic.Message}");
    }

    private static string FormatPath(string path)
    {
        try
        {
            return Path.IsPathFullyQualified(path)
                ? Path.GetRelativePath(Environment.CurrentDirectory, path)
                : path;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path;
        }
    }

    private bool TryGetLineColumn(string path, int position, out int line, out int column)
    {
        line = 0;
        column = 0;

        if (!_contents.TryGetValue(path, out string? content))
        {
            try
            {
                content = File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                content = null;
            }

            _contents[path] = content;
        }

        if (content == null || position > content.Length)
        {
            return false;
        }

        int lineNumber = 1;
        int lineStart = 0;
        for (int i = 0; i < position; i++)
        {
            if (content[i] == '\n')
            {
                lineNumber++;
                lineStart = i + 1;
            }
        }

        line = lineNumber;
        column = position - lineStart + 1;
        return true;
    }
}
