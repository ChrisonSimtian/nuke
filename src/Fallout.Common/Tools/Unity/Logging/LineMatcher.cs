using System.Text.RegularExpressions;

namespace Fallout.Common.Tools.Unity.Logging;

internal class LineMatcher
{
    public string RegexPattern { get; }

    public LogLevel LogLevel { get; }

    public LineMatcher(string regexPattern, LogLevel logLevel)
    {
        RegexPattern = regexPattern;
        LogLevel = logLevel;
    }

    public bool Matches(string message)
    {
        return Regex.IsMatch(message, RegexPattern);
    }
}
