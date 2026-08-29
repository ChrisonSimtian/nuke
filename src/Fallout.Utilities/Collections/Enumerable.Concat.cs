using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Fallout.Common.Utilities.Collections;

[SuppressMessage("ReSharper", "MissingXmlDoc")]
[DebuggerNonUserCode]
[DebuggerStepThrough]
public static partial class EnumerableExtensions
{
    public static IEnumerable<T> Concat<T>(this T obj, IEnumerable<T> enumerable)
    {
        yield return obj;

        foreach (var element in enumerable)
        {
            yield return element;
        }
    }

    public static IEnumerable<T> Concat<T>(this IEnumerable<T> enumerable, params T[] others)
    {
        foreach (var element in enumerable)
        {
            yield return element;
        }

        foreach (var element in others)
        {
            yield return element;
        }
    }
}
