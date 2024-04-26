using System.Text.RegularExpressions;
using RegexMatch = System.Text.RegularExpressions.Match;

namespace Needlefish.Lexing;

internal class TokenDefinition<T>
{
    private readonly Regex _regex;

    public readonly T Type;

    public TokenDefinition(T type, string regexPattern)
    {
        Type = type;
        _regex = new Regex(regexPattern);
    }

    public Match<T> Match(string input, int startIndex = 0)
    {
        RegexMatch match = _regex.Match(input, startIndex);
        if (!match.Success)
        {
            return default;
        }

        var token = new Token<T>(Type, match.Value);
        return new Match<T>(true, match.Length, token);
    }
}