using System;
using System.Collections.Generic;
using System.Linq;

namespace Fallout.Common.Utilities.Collections;

public static partial class EnumerableExtensions
{
    public static Dictionary<TKey, TValue> ToDictionary<T, TKey, TValue>(
        this IEnumerable<T> enumerable,
        Func<T, TKey> keySelector,
        Func<T, TValue> valueSelector,
        IEqualityComparer<TKey> comparer = null,
        Func<ArgumentException, TKey, Exception> exceptionFactory = null)
    {
        var list = enumerable.ToList();
        var dictionary = new Dictionary<TKey, TValue>(list.Count, comparer);

        foreach (var item in list)
        {
            var key = keySelector.Invoke(item);
            try
            {
                dictionary.Add(key, valueSelector.Invoke(item));
            }
            catch (ArgumentException exception) when (exceptionFactory != null)
            {
                throw exceptionFactory.Invoke(exception, key);
            }
        }

        return dictionary;
    }

    public static Dictionary<TKey, TValue> ToDictionarySafe<T, TKey, TValue>(
        this IEnumerable<T> enumerable,
        Func<T, TKey> keySelector,
        Func<T, TValue> valueSelector,
        string duplicationMessage)
    {
        var groups = enumerable.ToLookup(keySelector.Invoke, valueSelector.Invoke);
        Assert.True(
            groups.All(x => x.Count() == 1),
            new[]
                {
                    $"{duplicationMessage.TrimEnd(":")}:"
                }
                .Concat(groups.Where(x => x.Count() > 1).Select(x => $" - {x.Key}"))
                .JoinNewLine());

        return groups.ToDictionary(x => x.Key, x => x.Single());
    }
}
