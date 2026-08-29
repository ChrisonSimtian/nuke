namespace Fallout.Common.Utilities;

public static partial class StringExtensions
{
    /// <summary>
    /// Trims any multi-occurrence of a string in another string to a single-occurrence.
    /// </summary>
    public static string TrimToOne(this string str, string trim)
    {
        while (str.Contains(trim + trim))
        {
            str = str.Replace(trim + trim, trim);
        }

        return str;
    }

    /// <summary>
    /// Trims all whitespaces to single spaces.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string TrimWhitespaces(this string str)
    {
        return str.Replace("\r", string.Empty).Replace("\n", string.Empty).TrimToOne(" ");
    }

    /// <summary>
    /// Trims the occurrence of a string from the end of another string.
    /// </summary>
    public static string TrimEnd(this string str, string trim)
    {
        return str.EndsWith(trim) ? str.Substring(startIndex: 0, str.Length - trim.Length) : str;
    }

    /// <summary>
    /// Trims the occurrence of a string from the start of another string.
    /// </summary>
    public static string TrimStart(this string str, string trim)
    {
        return str.StartsWith(trim) ? str.Substring(trim.Length) : str;
    }

    /// <summary>
    /// Trims matching double-quotes from the start and end of a string.
    /// </summary>
    public static string TrimMatchingDoubleQuotes(this string str)
    {
        return str.TrimMatchingQuotes(quote: '"');
    }

    /// <summary>
    /// Trims matching double-quotes from the start and end of a string.
    /// </summary>
    public static string TrimMatchingSingleQuotes(this string str)
    {
        return str.TrimMatchingQuotes(quote: '\'');
    }

    internal static string TrimMatchingQuotes(this string str, char quote)
    {
        if (str.Length < 2)
        {
            return str;
        }

        if (str[index: 0] != quote || str[str.Length - 1] != quote)
        {
            return str;
        }

        return str.Substring(startIndex: 1, str.Length - 2);
    }
}
