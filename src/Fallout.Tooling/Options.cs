using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.Tooling;

public interface IOptions
{
    T Get<T>(Expression<Func<object>> provider);

    void Set<T>(Expression<Func<T>> provider, object value);
    void Remove<T>(Expression<Func<T>> provider);

    void SetDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> provider, TKey key, TValue value);
    void AddDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> provider, TKey key, TValue value);

    void AddDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> provider,
        Dictionary<TKey, TValue> value);

    void AddDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> provider,
        ReadOnlyDictionary<TKey, TValue> value);

    void RemoveDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> provider, TKey key);
    void ClearDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> provider);

    void SetLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider, TKey key, params TValue[] values);
    void SetLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider, TKey key, IEnumerable<TValue> values);
    void AddLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider, TKey key, params TValue[] values);
    void AddLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider, TKey key, IEnumerable<TValue> values);
    void RemoveLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider, TKey key);
    void RemoveLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider, TKey key, TValue value);
    void ClearLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider);

    void AddCollection<T>(Expression<Func<IReadOnlyCollection<T>>> provider, params T[] value);
    void AddCollection<T>(Expression<Func<IReadOnlyCollection<T>>> provider, IEnumerable<T> value);
    void RemoveCollection<T>(Expression<Func<IReadOnlyCollection<T>>> provider, params T[] value);
    void RemoveCollection<T>(Expression<Func<IReadOnlyCollection<T>>> provider, IEnumerable<T> value);
    void ClearCollection<T>(Expression<Func<IReadOnlyCollection<T>>> provider);
}

public class Options : IOptions
{
    // STJ does NOT inherit class-level [JsonConverter] attributes onto subclasses, so the
    // Options→InternalOptions redirect lives in a JsonConverterFactory registered in the static
    // SerializerOptions below. CanConvert matches Options and every subclass (ToolOptions and the
    // 62 generated *Settings types).
    public class TypeConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(Options) || typeToConvert.IsSubclassOf(typeof(Options));
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(InnerConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType).NotNull();
        }

        private class InnerConverter<T> : JsonConverter<T>
            where T : Options
        {
            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                value.InternalOptions.WriteTo(writer, options);
            }

            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var instance = (T)typeToConvert.CreateInstance();
                instance.InternalOptions = JsonNode.Parse(ref reader)?.AsObject() ?? new JsonObject();
                return instance;
            }
        }
    }

    internal static JsonConverter LookupTableConverter = new ObjectFromFieldConverter(typeof(LookupTable<,>), "lookupDictionary");

    internal static JsonSerializerOptions SerializerOptions { get; } = CreateSerializerOptions();

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            WriteIndented = true,
        };

        options.Converters.Add(new TypeConverter());
        options.Converters.Add(LookupTableConverter);
        options.Converters.Add(new EnumerationJsonConverterFactory());
        return options;
    }

    protected internal JsonObject InternalOptions = new();

    private static string GetOptionName(LambdaExpression lambdaExpression)
    {
        var member = lambdaExpression.GetMemberInfo();
        return member.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? member.Name;
    }

    void IOptions.Set<T>(Expression<Func<T>> provider, object value)
    {
        Set(GetOptionName(provider), value);
    }

    protected internal void Set<T>(Expression<Func<T>> provider, object value)
    {
        ((IOptions)this).Set(provider, value);
    }

    internal void Set(string propertyName, object value)
    {
        if (value != null)
        {
            var internalOption = JsonSerializer.SerializeToNode(value, value.GetType(), SerializerOptions);
            InternalOptions[propertyName] = internalOption;
        }
        else
        {
            InternalOptions.Remove(propertyName);
        }
    }

    void IOptions.Remove<T>(Expression<Func<T>> provider)
    {
        InternalOptions.Remove(GetOptionName(provider));
    }

    T IOptions.Get<T>(Expression<Func<object>> provider)
    {
        return Get<T>((LambdaExpression)provider);
    }

    protected T Get<T>(Expression<Func<object>> provider)
    {
        return Get<T>((LambdaExpression)provider);
    }

    private T Get<T>(LambdaExpression provider)
    {
        return InternalOptions[GetOptionName(provider)] is { } node ? node.Deserialize<T>(SerializerOptions) : default;
    }

    #region Dictionary

    private void UsingDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> provider,
        Action<Dictionary<TKey, TValue>> action)
    {
        var dictionary = Get<Dictionary<TKey, TValue>>(provider) ?? new Dictionary<TKey, TValue>();
        action.Invoke(dictionary);
        Set(provider, dictionary);
    }

    void IOptions.SetDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> provider, TKey key,
        TValue value)
    {
        UsingDictionary(provider, dictionary => dictionary[key] = value);
    }

    void IOptions.AddDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> provider, TKey key,
        TValue value)
    {
        UsingDictionary(provider, dictionary => dictionary.Add(key, value));
    }

    void IOptions.AddDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> provider,
        Dictionary<TKey, TValue> value)
    {
        UsingDictionary(provider, dictionary => dictionary.AddDictionary(value));
    }

    void IOptions.AddDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> provider,
        ReadOnlyDictionary<TKey, TValue> value)
    {
        UsingDictionary(provider, dictionary => dictionary.AddDictionary(value));
    }

    void IOptions.RemoveDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> provider, TKey key)
    {
        UsingDictionary(provider, dictionary => dictionary.Remove(key));
    }

    void IOptions.ClearDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> provider)
    {
        UsingDictionary(provider, dictionary => dictionary.Clear());
    }

    #endregion

    #region Lookup

    private void UsingLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider,
        Action<LookupTable<TKey, TValue>> action)
    {
        var lookup = Get<LookupTable<TKey, TValue>>(provider) ?? new LookupTable<TKey, TValue>();
        action.Invoke(lookup);
        Set(provider, lookup);
    }

    void IOptions.SetLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider, TKey key, params TValue[] values)
    {
        UsingLookup(provider, lookup => lookup[key] = values);
    }

    void IOptions.SetLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider, TKey key, IEnumerable<TValue> values)
    {
        UsingLookup(provider, lookup => lookup[key] = values);
    }

    void IOptions.AddLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider, TKey key, params TValue[] values)
    {
        UsingLookup(provider, lookup => lookup.AddRange(key, values));
    }

    void IOptions.AddLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider, TKey key, IEnumerable<TValue> values)
    {
        UsingLookup(provider, lookup => lookup.AddRange(key, values));
    }

    void IOptions.RemoveLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider, TKey key)
    {
        UsingLookup(provider, lookup => lookup.Remove(key));
    }

    void IOptions.RemoveLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider, TKey key, TValue value)
    {
        UsingLookup(provider, lookup => lookup.Remove(key, value));
    }

    void IOptions.ClearLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> provider)
    {
        UsingLookup(provider, lookup => lookup.Clear());
    }

    #endregion

    #region List

    private void UsingCollection<T>(Expression<Func<IReadOnlyCollection<T>>> provider, Action<List<T>> action)
    {
        var collection = Get<List<T>>(provider) ?? new List<T>();
        action.Invoke(collection);
        Set(provider, collection);
    }

    void IOptions.AddCollection<T>(Expression<Func<IReadOnlyCollection<T>>> provider, params T[] value)
    {
        UsingCollection(provider, collection => collection.AddRange(value));
    }

    void IOptions.AddCollection<T>(Expression<Func<IReadOnlyCollection<T>>> provider, IEnumerable<T> value)
    {
        UsingCollection(provider, collection => collection.AddRange(value));
    }

    void IOptions.RemoveCollection<T>(Expression<Func<IReadOnlyCollection<T>>> provider, params T[] value)
    {
        UsingCollection(provider, collection => collection.RemoveAll(value.Contains));
    }

    void IOptions.RemoveCollection<T>(Expression<Func<IReadOnlyCollection<T>>> provider, IEnumerable<T> value)
    {
        UsingCollection(provider, collection => collection.RemoveAll(value.ToList().Contains));
    }

    void IOptions.ClearCollection<T>(Expression<Func<IReadOnlyCollection<T>>> provider)
    {
        UsingCollection(provider, collection => collection.Clear());
    }

    #endregion
}
