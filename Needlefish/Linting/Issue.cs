namespace Needlefish.Linting;

internal readonly struct Issue
{
    public readonly string Message;
    public readonly IssueLevel Level;

    public Issue(string message, IssueLevel level = IssueLevel.Error)
    {
        Message = message;
        Level = level;
    }
}