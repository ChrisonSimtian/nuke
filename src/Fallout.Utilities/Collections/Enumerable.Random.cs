using System;
using System.Collections.Generic;
using System.Linq;

namespace Fallout.Common.Utilities.Collections;

partial class EnumerableExtensions
{
    private static readonly Random randomNumberGenerator = new();

    public static T Random<T>(this IEnumerable<T> collection)
    {
        var array = collection.ToArray();
        return array[randomNumberGenerator.Next(array.Length)];
    }

    public static ICollection<T> Randomize<T>(this ICollection<T> collection)
    {
        var list = collection.ToList();
        var count = list.Count;
        while (count > 1)
        {
            count--;
            var k = randomNumberGenerator.Next(count + 1);
            (list[k], list[count]) = (list[count], list[k]);
        }

        return list;
    }
}
