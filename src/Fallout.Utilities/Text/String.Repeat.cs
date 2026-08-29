namespace Fallout.Common.Utilities;

public static partial class StringExtensions
{
    public static string Repeat(this char ch, int count)
    {
        return new string(ch, count);
    }
}
