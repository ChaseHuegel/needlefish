namespace Needlefish.Linting;

internal readonly struct Issue
{
    public readonly string Message;
    public readonly IssueLevel Level;
    public readonly int Position;
    public readonly int Length;

    public Issue(string message, IssueLevel level = IssueLevel.Error, int position = -1, int length = 0)
    {
        Message = message;
        Level = level;
        Position = position;
        Length = length;
    }
}
