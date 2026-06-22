using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.Tooling;

public class CommandAttribute : Attribute
{
    public Type Type { get; set; }
    public string Command { get; set; }
    public string Arguments { get; set; }
}

public class ArgumentEscapeAttribute : Attribute
{
    public Type Type { get; set; }
    public string Method { get; set; }
}

public class ArgumentAttribute : Attribute
{
    public int Position { get; set; }
    public string Format { get; set; }
    public string AlternativeFormat { get; set; }
    public bool Secret { get; set; }

    public Type FormatterType { get; set; }
    public string FormatterMethod { get; set; }

    public string Separator { get; set; }
    public string InnerSeparator { get; set; }
    public bool QuoteMultiple { get; set; }
}

public class BuilderAttribute : Attribute
{
    public Type Type { get; set; }
    public string Property { get; set; }
}

partial class ToolOptions
{
    private const string KeyPlaceholder = "{key}";
    private const string ValuePlaceholder = "{value}";

    internal partial IEnumerable<string> GetArguments()
    {
        var commandAttribute = GetType().GetCustomAttribute<CommandAttribute>();
        if (commandAttribute?.Arguments != null)
            yield return commandAttribute.Arguments;

        var escapeMethod = CreateEscape();
        var arguments = InternalOptions
            .Select(kv => (Node: kv.Value, Property: _allProperties[kv.Key]))
            .Select(x => (x.Node, x.Property, Attribute: x.Property.GetCustomAttribute<ArgumentAttribute>()))
            .Where(x => x.Attribute != null)
            .OrderByDescending(x => x.Attribute.Position.CompareTo(0))
            .ThenBy(x => x.Attribute.Position)
            .SelectMany(x => GetArgument(x.Node, x.Property, x.Attribute, escapeMethod))
            .WhereNotNull()
            .Concat(ProcessAdditionalArguments ?? []);

        foreach (var argument in arguments)
            yield return argument;

        Func<string, PropertyInfo, string> CreateEscape()
        {
            var toolType = commandAttribute?.Type;
            var escapeAttribute = toolType?.GetCustomAttribute<ArgumentEscapeAttribute>();
            if (escapeAttribute == null)
                return (x, _) => x.DoubleQuoteIfNeeded();

            var escapeType = escapeAttribute.Type ?? GetType();
            var formatterMethod = escapeType.GetMethod(escapeAttribute.Method, ReflectionUtility.All);
            return (value, property) => formatterMethod.GetValue<string>(obj: this, args: [value, property]);
        }
    }

    private IEnumerable<string> GetArgument(
        JsonNode node,
        PropertyInfo property,
        ArgumentAttribute attribute,
        Func<string, PropertyInfo, string> escape)
    {
        var format = attribute.Format;
        var formatParts = format.SplitSpace();

        if (!property.PropertyType.IsGenericType || property.PropertyType.IsNullableType())
            return GetScalarArguments();

        if (property.PropertyType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            return GetListArguments();

        if (property.PropertyType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
            return GetDictionaryArguments();

        if (property.PropertyType.GetGenericTypeDefinition() == typeof(ILookup<,>))
            return GetLookupArguments();

        return [];

        string Parse(JsonNode token, Type type)
        {
            string value;
            if (attribute.FormatterMethod != null)
            {
                var formatterType = attribute.FormatterType ?? GetType();
                var formatterMethod = formatterType.GetMethod(attribute.FormatterMethod, ReflectionUtility.All);
                var objValue = type != typeof(object) ? DeserializeWithCoercion(token, type) : NodeToString(token);
                // The PropertyInfo is an optional second argument: formatters that don't need it
                // can declare just the value parameter. Pass args matching the method's arity so
                // both `Format(value)` and `Format(value, PropertyInfo)` shapes are supported.
                object[] formatterArgs = formatterMethod.GetParameters().Length == 1
                    ? [objValue]
                    : [objValue, property];
                value = formatterMethod.GetValue<string>(obj: this, args: formatterArgs);
            }
            else
            {
                value = NodeToString(token);
                if (new[] { typeof(bool), typeof(bool?) }.Contains(type) ||
                    (type == typeof(object) && new[] { bool.TrueString, bool.FalseString }.Contains(value)))
                    value = value?.ToLowerInvariant();
            }

            return escape.Invoke(value, property);
        }

        IEnumerable<string> GetScalarArguments()
        {
            if (property.PropertyType == typeof(bool?) &&
                !format.ContainsOrdinalIgnoreCase(ValuePlaceholder) &&
                !node.GetValue<bool>())
                yield break;

            var value = Parse(node, property.PropertyType);
            if (value.IsNullOrWhiteSpace())
                yield break;

            foreach (var part in formatParts)
                yield return part.Replace(ValuePlaceholder, value);
        }

        IEnumerable<string> GetListArguments()
        {
            Assert.True(formatParts.Length <= 2);

            var valueType = property.PropertyType.GetScalarType();
            var values = node.AsArray().Select(x => Parse(x, valueType)).Where(x => !x.IsNullOrWhiteSpace());

            if (attribute.Separator == null)
            {
                Assert.False(attribute.QuoteMultiple);
                return from value in values
                       from part in formatParts
                       select part.Replace(ValuePlaceholder, value);
            }

            return formatParts.SelectMany(part =>
            {
                var valueStart = part.IndexOf(ValuePlaceholder, StringComparison.OrdinalIgnoreCase);
                if (valueStart == -1)
                    return [part];

                if (attribute.Separator.IsNullOrWhiteSpace() && !attribute.QuoteMultiple)
                    return values.Select(x => part.Replace(ValuePlaceholder, x));

                var joinedValues = values.Join(attribute.Separator);
                if (attribute.QuoteMultiple)
                    joinedValues = joinedValues.DoubleQuote();

                return [part.Replace(ValuePlaceholder, joinedValues)];
            });
        }

        IEnumerable<string> GetDictionaryArguments()
        {
            var valueType = property.PropertyType.GetGenericArguments().Last();
            var pairs = node.AsObject().Select(kv => (Key: kv.Key, Value: Parse(kv.Value, valueType)))
                .Where(x => !x.Value.IsNullOrWhiteSpace());

            if (attribute.Separator == null)
            {
                return from pair in pairs
                       from part in formatParts
                       select part.Replace(KeyPlaceholder, pair.Key).Replace(ValuePlaceholder, pair.Value);
            }

            return formatParts.SelectMany(part =>
            {
                var pairStart = part.IndexOf(KeyPlaceholder, StringComparison.OrdinalIgnoreCase);
                var pairLength = part.IndexOf(ValuePlaceholder, StringComparison.OrdinalIgnoreCase) - pairStart + ValuePlaceholder.Length;

                if (pairStart == -1)
                    return [part];

                if (attribute.Separator.IsNullOrWhiteSpace())
                    return pairs.Select(x => part.Replace(KeyPlaceholder, x.Key).Replace(ValuePlaceholder, x.Value));

                var pairPart = part.Substring(pairStart, pairLength);
                var replacedPairs = pairs.Select(x => pairPart.Replace(KeyPlaceholder, x.Key).Replace(ValuePlaceholder, x.Value));
                return [part.Substring(startIndex: 0, length: pairStart) + replacedPairs.Join(attribute.Separator)];
            });
        }

        IEnumerable<string> GetLookupArguments()
        {
            var valueType = property.PropertyType.GetGenericArguments().Last();
            var pairs = node.AsObject()
                .Select(kv => (Key: kv.Key, Values: kv.Value.AsArray().Select(x => Parse(x, valueType))));

            if (attribute.Separator == null)
            {
                if (attribute.InnerSeparator == null)
                {
                    return from pair in pairs
                           from value in pair.Values
                           from part in formatParts
                           select part.Replace(KeyPlaceholder, pair.Key).Replace(ValuePlaceholder, value);
                }
                else
                {
                    return from pair in pairs
                           from part in formatParts
                           select part.Replace(KeyPlaceholder, pair.Key).Replace(ValuePlaceholder, pair.Values.Join(attribute.InnerSeparator));
                }
            }

            Assert.NotNull(attribute.InnerSeparator);

            return formatParts.SelectMany(part =>
            {
                var pairStart = part.IndexOf(KeyPlaceholder, StringComparison.OrdinalIgnoreCase);
                var pairLength = part.IndexOf(ValuePlaceholder, StringComparison.OrdinalIgnoreCase) - pairStart + ValuePlaceholder.Length;

                if (pairStart == -1)
                    return [part];

                if (attribute.Separator.IsNullOrWhiteSpace())
                    return pairs.Select(x => part.Replace(KeyPlaceholder, x.Key).Replace(ValuePlaceholder, x.Values.Join(attribute.InnerSeparator))).ToArray();

                var pairPart = part.Substring(pairStart, pairLength);
                var replacedPairs = pairs.Select(x => pairPart.Replace(KeyPlaceholder, x.Key).Replace(ValuePlaceholder, x.Values.Join(attribute.InnerSeparator)));
                return [part.Substring(startIndex: 0, length: pairStart) + replacedPairs.Join(attribute.Separator)];
            });
        }
    }

    // Mirrors Newtonsoft's permissive JToken.ToObject<string>(): unwraps the JSON value to a plain
    // string regardless of underlying kind. STJ's GetValue<string>() throws on non-string nodes.
    private static string NodeToString(JsonNode node)
    {
        if (node is null) return null;
        return node.GetValueKind() switch
        {
            JsonValueKind.String => node.GetValue<string>(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null => null,
            _ => node.ToString()
        };
    }

    // Newtonsoft's JToken.ToObject<T> coerced string JSON values into target primitives (e.g. "true" -> bool).
    // STJ refuses. Bridge that one quirk so test fixtures and tool wrappers passing string-typed bool values
    // through the formatter-method path keep working. DateTime/numeric/etc. are handled correctly by STJ's
    // native string-to-T deserialization, so we only intervene for bool.
    private static object DeserializeWithCoercion(JsonNode token, Type type)
    {
        if (token is JsonValue jv && jv.GetValueKind() == JsonValueKind.String)
        {
            var target = Nullable.GetUnderlyingType(type) ?? type;
            if (target == typeof(bool) && bool.TryParse(jv.GetValue<string>(), out var coerced))
                return coerced;
        }
        return token.Deserialize(type, Options.SerializerOptions);
    }
}
