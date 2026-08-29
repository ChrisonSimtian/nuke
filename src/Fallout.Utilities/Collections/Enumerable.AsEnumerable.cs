using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fallout.Common.Utilities.Collections;

public static partial class EnumerableExtensions
{
    /// <summary>
    /// Create an enumerable with the object as single element.
    /// </summary>
    public static IEnumerable<T> AsEnumerable<T>(this object obj)
    {
        return obj is IEnumerable enumerable
            ? enumerable.Cast<T>()
            : new[]
            {
                (T)obj
            };
    }
}
