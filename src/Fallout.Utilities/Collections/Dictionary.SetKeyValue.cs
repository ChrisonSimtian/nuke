using System.Collections.Generic;

namespace Fallout.Common.Utilities.Collections;

public static partial class DictionaryExtensions
{
    public static IDictionary<TKey, TValue> SetKeyValue<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        TValue value = default)
    {
        dictionary[key] = value;
        return dictionary;
    }
}
