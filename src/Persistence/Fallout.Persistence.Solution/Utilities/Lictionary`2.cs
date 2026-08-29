// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Linq;

namespace Fallout.Persistence.Solution.Utilities;

/// <summary>
/// Provides some dictionary like functionality with a list of key value pairs.
/// Used for small collections where the overhead of a dictionary is too high.
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
internal readonly struct Lictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private static readonly EntryKeyComparer DefaultComparer = new(Comparer<TKey>.Default);

    private readonly List<KeyValuePair<TKey, TValue>> items;
    private readonly EntryKeyComparer comparer;

    public Lictionary()
        : this(capacity: 0, comparer: null)
    {
    }

    internal Lictionary(int capacity, IComparer<TKey>? comparer = null)
    {
        this.comparer = comparer is null ? DefaultComparer : new EntryKeyComparer(comparer);
        items = new List<KeyValuePair<TKey, TValue>>(capacity);
    }

    internal Lictionary(IReadOnlyCollection<KeyValuePair<TKey, TValue>> values, IComparer<TKey>? comparer = null)
    {
        Argument.ThrowIfNull(values, nameof(values));
        this.comparer = comparer is null ? DefaultComparer : new EntryKeyComparer(comparer);
        items = [.. values];
        items.Sort(this.comparer);

        KeyValuePair<TKey, TValue> lastEntry = default;
        foreach (KeyValuePair<TKey, TValue> entry in items)
        {
            if (this.comparer.Equals(lastEntry, entry))
            {
                throw new ArgumentException(Errors.DuplicateKey, nameof(values));
            }

            lastEntry = entry;
        }
    }

    public IEnumerable<TKey> Keys => this.Select(x => x.Key);

    public IEnumerable<TValue> Values => this.Select(x => x.Value);

    public int Count => items.Count;

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? value) ? value : throw new KeyNotFoundException(nameof(key));
        set
        {
            int index = BinarySearch(key);
            if (index >= 0)
            {
                items[index] = new KeyValuePair<TKey, TValue>(key, value);
            }
            else
            {
                items.Insert(~index, new KeyValuePair<TKey, TValue>(key, value));
            }
        }
    }

    public TValue this[int index] => items[index].Value;

    public bool ContainsKey(TKey key) => BinarySearch(key) >= 0;

#if NETFRAMEWORK || NETSTANDARD
#nullable disable warnings
#endif
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#if NETFRAMEWORK || NETSTANDARD
#nullable restore
#endif
    {
        int index = BinarySearch(key);
        if (index >= 0)
        {
            value = items[index].Value;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();

    public List<KeyValuePair<TKey, TValue>>.Enumerator GetEnumerator() => items.GetEnumerator();

    internal void Add(TKey key, TValue value)
    {
        if (!TryAdd(key, value))
        {
            throw new ArgumentException(Errors.DuplicateKey, nameof(key));
        }
    }

    internal bool TryAdd(TKey key, TValue value)
    {
        int index = BinarySearch(key);
        if (index >= 0)
        {
            return false;
        }
        else
        {
            items.Insert(~index, new KeyValuePair<TKey, TValue>(key, value));
            return true;
        }
    }

    internal bool Remove(TKey key)
    {
        int index = BinarySearch(key);
        if (index >= 0)
        {
            items.RemoveAt(index);
            return true;
        }

        return false;
    }

    internal void Clear() => items.Clear();

    internal bool TryFindNext(TKey key, [MaybeNullWhen(false)] out TValue? value)
    {
        int index = ~BinarySearch(key);
        if (index >= 0 && index < items.Count)
        {
            value = items[index].Value;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    internal void EnsureCapacity(int capacity)
    {
#if NETFRAMEWORK || NETSTANDARD
        if (capacity > items.Capacity)
        {
            items.Capacity = capacity;
        }
#else
        _ = this.items.EnsureCapacity(capacity);
#endif
    }

    private int BinarySearch(TKey key)
    {
        Argument.ThrowIfNull(key, nameof(key));
        return items.BinarySearch(new KeyValuePair<TKey, TValue>(key, default!), comparer);
    }

    private sealed class EntryKeyComparer(IComparer<TKey> keyComparer) : IComparer<KeyValuePair<TKey, TValue>>
    {
        public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) =>
            keyComparer.Compare(x.Key, y.Key);
    }
}
