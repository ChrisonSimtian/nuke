using System.Collections.Generic;

namespace Fallout.Common.Utilities.Collections;

public static partial class DictionaryExtensions
{
    public static Dictionary<TKey, TValue> AddPair<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        TValue value = default)
    {
        dictionary.Add(key, value);
        return dictionary;
    }

    public static Dictionary<TKey, string> AddPair<TKey, TValue>(
        this Dictionary<TKey, string> dictionary,
        TKey key,
        TValue value = default)
    {
        dictionary.Add(key, value.ToString());
        return dictionary;
    }

    public static Dictionary<TKey, TValue> AddPairWhenKeyNotNull<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        TValue value = default)
        where TKey : class
    {
        return key != null
            ? dictionary.AddPair(key, value)
            : dictionary;
    }

    public static Dictionary<TKey, TValue> AddPairWhenValueNotNull<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        TValue value)
        where TValue : class
    {
        return value != null
            ? dictionary.AddPair(key, value)
            : dictionary;
    }
}
