using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fallout.Common.Utilities;

namespace Fallout.Common.Tooling;

public static class DelegateHelper
{
    public static IDictionary<string, object> Toggle(IReadOnlyDictionary<string, object> dictionary, string key)
    {
        var newDictionary = dictionary?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, object>();
        newDictionary[key] = !newDictionary.ContainsKey(key) || !ReflectionUtility.Convert<bool>(newDictionary[key].ToString());
        return newDictionary;
    }

    public static IDictionary<string, object> SetCollection<TValue>(
        IReadOnlyDictionary<string, object> dictionary,
        string key,
        IEnumerable<TValue> values,
        string separator)
    {
        var newDictionary = dictionary?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, object>();
        var collectionAsString = CollectionToString(values, separator);
        if (!string.IsNullOrWhiteSpace(collectionAsString))
        {
            newDictionary[key] = collectionAsString;
        }

        return newDictionary;
    }

    public static IDictionary<string, object> AddCollection<TValue>(
        IReadOnlyDictionary<string, object> dictionary,
        string key,
        IEnumerable<TValue> values,
        string separator)
    {
        var newDictionary = dictionary?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, object>();
        var collection = ParseCollection<TValue>(dictionary, key, separator);
        collection.AddRange(values);
        newDictionary[key] = CollectionToString(collection, separator);
        return newDictionary;
    }

    public static IDictionary<string, object> RemoveCollection<TValue>(
        IReadOnlyDictionary<string, object> dictionary,
        string key,
        IEnumerable<TValue> values,
        string separator)
    {
        var newDictionary = dictionary?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, object>();
        var valueHashSet = new HashSet<TValue>(values);
        var collection = ParseCollection<TValue>(dictionary, key, separator);
        collection.RemoveAll(x => valueHashSet.Contains(x));
        newDictionary[key] = CollectionToString(collection, separator);
        return newDictionary;
    }

    private static List<TValue> ParseCollection<TValue>(IReadOnlyDictionary<string, object> dictionary, string key,
        string separator)
    {
        if (dictionary?.TryGetValue(key, out var value) != true)
        {
            return [];
        }

        // STJ round-tripping IReadOnlyDictionary<string, object> deserializes values as JsonElement,
        // not as primitive strings like Newtonsoft did. Coerce both shapes back to string here.
        var stringValue = value switch
        {
            string s => s,
            JsonElement el when el.ValueKind == JsonValueKind.String => el.GetString(),
            null => null,
            _ => value.ToString()
        };

        return (stringValue?.Split([separator], StringSplitOptions.RemoveEmptyEntries) ?? [])
            .Select(ReflectionUtility.Convert<TValue>).ToList();
    }

    private static string CollectionToString<T>(IEnumerable<T> collection, string separator)
    {
        return collection.Select(x => x.ToString()).Join(separator);
    }
}
