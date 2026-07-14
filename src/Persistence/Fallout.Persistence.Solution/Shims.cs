namespace Fallout.Persistence.Solution;

public static class PathShim
{
    public static char DirectorySeparatorChar => System.IO.Path.DirectorySeparatorChar;

    public static char AltDirectorySeparatorChar => System.IO.Path.AltDirectorySeparatorChar;

    public static string? GetDirectoryName(string value) => System.IO.Path.GetDirectoryName(value);

    public static StringSpan GetExtension(StringSpan span) => System.IO.Path.GetExtension(span.ToString()).AsSpan();

    public static StringSpan GetFileNameWithoutExtension(StringSpan span) =>
        System.IO.Path.GetFileNameWithoutExtension(span.ToString()).AsSpan();
}

public static class Extensions
{
    public static bool Contains(this string str, char c)
    {
        return str.Contains(c.ToString());
    }

    public static bool TryGetValue<T>(this HashSet<T> set, T equalValue, out T? actualValue)
    {
        foreach (var item in set)
        {
            if (set.Comparer.Equals(item, equalValue))
            {
                actualValue = item;
                return true;
            }
        }

        actualValue = default;
        return false;
    }
}
