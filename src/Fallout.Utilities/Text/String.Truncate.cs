namespace Fallout.Common.Utilities;

partial class StringExtensions
{
    public static string Truncate(this string str, int maxChars)
    {
        return str.Length <= maxChars ? str : str.Substring(0, maxChars) + "…";
    }
}
