#if NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fallout.Common.Utilities.Collections;

[DebuggerStepThrough]
[DebuggerNonUserCode]
public static partial class DictionaryExtensions
{
    internal static TValue GetValueOrDefault<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> dictionary,
        TKey key,
        TValue defaultValue = default)
    {
        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }
}
#endif
