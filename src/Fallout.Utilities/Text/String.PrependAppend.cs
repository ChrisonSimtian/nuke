namespace Fallout.Common.Utilities;

public static partial class StringExtensions
{
    /// <summary>
    /// Prepends a string to another string.
    /// </summary>
    public static string Prepend(this string str, string prependText)
    {
        return prependText + str;
    }

    /// <summary>
    /// Appends a string to another string.
    /// </summary>
    public static string Append(this string str, string appendText)
    {
        return str + appendText;
    }
}
