using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fallout.Common.Utilities;

public static partial class StringExtensions
{
    /// <summary>
    /// Replaces matches of regular expressions.
    /// </summary>
    public static string ReplaceRegex(
        this string str,
        string pattern,
        MatchEvaluator matchEvaluator,
        RegexOptions options = RegexOptions.None)
    {
        return Regex.Replace(str, pattern, matchEvaluator, options);
    }

    private static readonly Regex unicodeRegex = new(@"\\u(?<Value>[a-zA-Z0-9]{4})", RegexOptions.Compiled);

    public static string ReplaceUnicode(this string str)
    {
        return unicodeRegex.Replace(str, m => ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString());
    }

    public static string ReplaceKnownWords(this string str)
    {
        return KnownWords.Aggregate(str, (s, r) => s.ReplaceRegex(r, _ => r, RegexOptions.IgnoreCase));
    }
}
