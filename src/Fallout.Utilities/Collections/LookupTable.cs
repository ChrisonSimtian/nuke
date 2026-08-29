using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fallout.Common.Utilities.Collections;

[Serializable]
public class LookupTable<TKey, TValue>(Dictionary<TKey, List<TValue>> dictionary) : ILookup<TKey, TValue>
{
    private readonly Dictionary<TKey, List<TValue>> lookupDictionary = dictionary;

    public LookupTable()
        : this(new Dictionary<TKey, List<TValue>>())
    {
    }

    public LookupTable(IEqualityComparer<TKey> comparer)
        : this(new Dictionary<TKey, List<TValue>>(comparer))
    {
    }

    public LookupTable(ILookup<TKey, TValue> lookupTable, IEqualityComparer<TKey> comparer = null)
        : this(lookupTable.ToDictionary(x => x.Key, x => x.ToList(), comparer))
    {
    }

    private ILookup<TKey, TValue> Lookup =>
        lookupDictionary.SelectMany(x => x.Value.Select(y => new KeyValuePair<TKey, TValue>(x.Key, y)))
            .ToLookup(x => x.Key, x => x.Value);

    public int Count => Lookup.Count;

    public IEnumerable<TValue> this[TKey key]
    {
        get => Lookup[key];
        set => lookupDictionary[key] = value.ToList();
    }

    public void Add(TKey key, TValue value)
    {
        var list = (lookupDictionary[key] = lookupDictionary.GetValueOrDefault(key, new List<TValue>())).NotNull();
        list.Add(value);
    }

    public void AddRange(TKey key, IEnumerable<TValue> values)
    {
        var list = (lookupDictionary[key] = lookupDictionary.GetValueOrDefault(key, new List<TValue>())).NotNull();
        foreach (var value in values)
        {
            list.Add(value);
        }
    }

    public void Remove(TKey key)
    {
        lookupDictionary.Remove(key);
    }

    public void Remove(TKey key, TValue value)
    {
        lookupDictionary.GetValueOrDefault(key)?.Remove(value);
    }

    public void Clear()
    {
        lookupDictionary.Clear();
    }

    public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
    {
        return Lookup.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Contains(TKey key)
    {
        return lookupDictionary.ContainsKey(key);
    }

    public ILookup<TKey, TValue> AsReadOnly()
    {
        return this;
    }
}
