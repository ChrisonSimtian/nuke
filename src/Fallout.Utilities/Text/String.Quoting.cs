using System;
using System.Linq;

namespace Fallout.Common.Utilities;

public static partial class StringExtensions
{
    /// <summary>
    /// Double-quotes a given string if it contains spaces. Empty and already quoted strings remain unchanged.
    /// </summary>
    public static string DoubleQuoteIfNeeded(this string str)
    {
        return str.DoubleQuoteIfNeeded(' ');
    }

    /// <summary>
    /// Double-quotes a given string if it contains disallowed characters. Empty and already quoted strings remain unchanged.
    /// </summary>
    public static string DoubleQuoteIfNeeded(this string str, params char?[] disallowed)
    {
        if (string.IsNullOrWhiteSpace(str))
            return string.Empty;

        if (str.IsDoubleQuoted())
            return str;

        if (!str.Contains(disallowed))
            return str;

        return str.DoubleQuote();
    }

    /// <summary>
    /// Double-quotes a given string in double-quotes with existing double-quotes escaped.
    /// </summary>
    public static string DoubleQuote(this string str)
    {
        return $"\"{str?.Replace("\"", "\\\"")}\"";
    }

    /// <summary>
    /// Single-quotes a given string if it contains spaces. Empty and already quoted strings remain unchanged.
    /// </summary>
    public static string SingleQuoteIfNeeded(this string str)
    {
        return str.SingleQuoteIfNeeded(' ');
    }

    /// <summary>
    /// Single-quotes a given string if it contains disallowed characters. Empty and already quoted strings remain unchanged.
    /// </summary>
    public static string SingleQuoteIfNeeded(this string str, params char?[] disallowed)
    {
        if (string.IsNullOrWhiteSpace(str))
            return string.Empty;

        if (str.IsSingleQuoted())
            return str;

        if (!str.Contains(disallowed))
            return str;

        return str.SingleQuote();
    }

    /// <summary>
    /// Single-quotes a given string with existing single-quotes escaped.
    /// </summary>
    public static string SingleQuote(this string str)
    {
        return $"'{str?.Replace("'", "\\'")}'";
    }

    /// <summary>
    /// Single-quotes a given string as a YAML flow scalar, escaping an embedded single quote by doubling it
    /// (<c>''</c>) per the YAML spec. Prefer this over <see cref="SingleQuote"/> when emitting YAML: the latter
    /// backslash-escapes, which is fine for shell/log output but invalid inside a YAML single-quoted scalar.
    /// </summary>
    public static string SingleQuoteYaml(this string str)
    {
        return $"'{str?.Replace("'", "''")}'";
    }

    /// <summary>
    /// Indicates whether a given string is double-quoted.
    /// </summary>
    public static bool IsDoubleQuoted(this string str)
    {
        return str.StartsWith("\"") && str.EndsWith("\"");
    }

    /// <summary>
    /// Indicates whether a given string is single-quoted.
    /// </summary>
    public static bool IsSingleQuoted(this string str)
    {
        return str.StartsWith("'") && str.EndsWith("'");
    }

    private static bool Contains(this string str, char?[] chars)
    {
        return chars.Any(x => x.HasValue && str.IndexOf(x.Value) != -1);
    }
}