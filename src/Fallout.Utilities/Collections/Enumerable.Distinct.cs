using System;
using System.Collections.Generic;

namespace Fallout.Common.Utilities.Collections;

public static partial class EnumerableExtensions
{
    /// <summary>
    /// Filters a collection to distinct/unique elements by a projected key.
    /// </summary>
    [Obsolete("Use MoreLinq.MoreEnumerable.DistinctBy (from the morelinq package) instead. This forwarder will be removed in a future major.")]
    public static IEnumerable<TSource> Distinct<TSource, TValue>(this IEnumerable<TSource> enumerable, Func<TSource, TValue> selector)
    {
        return MoreLinq.MoreEnumerable.DistinctBy(enumerable, selector);
    }
}
