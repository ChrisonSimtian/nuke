using System;
using System.Collections.Generic;
using System.Linq;

namespace Fallout.Common.Utilities.Collections;

public static partial class EnumerableExtensions
{
    private static readonly Random s_randomNumberGenerator = new Random();

    /// <summary>
    /// Returns a single random element from the collection. No MoreLINQ/BCL equivalent — kept.
    /// </summary>
    public static T Random<T>(this IEnumerable<T> collection)
    {
        var array = collection.ToArray();
        return array[s_randomNumberGenerator.Next(array.Length)];
    }

    /// <summary>
    /// Returns the elements of the collection in random order.
    /// </summary>
    [Obsolete("Use MoreLinq.MoreEnumerable.Shuffle (from the morelinq package) instead. This forwarder will be removed in a future major.")]
    public static ICollection<T> Randomize<T>(this ICollection<T> collection)
    {
        return MoreLinq.MoreEnumerable.Shuffle(collection).ToList();
    }
}
