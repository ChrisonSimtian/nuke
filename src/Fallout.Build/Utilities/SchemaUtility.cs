using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Fallout.Common.ValueInjection;
using static Fallout.Common.Constants;

namespace Fallout.Common.Execution;

/// <summary>
/// Generates a draft-04 JSON Schema for a build's <c>[Parameter]</c>-attributed members so editors
/// can autocomplete and validate the consumer's <c>parameters.json</c>. Hand-rolled on top of
/// <see cref="JsonNode"/> — no NJsonSchema, no Newtonsoft. The output shape is the draft-04 envelope
/// the NUKE ecosystem has emitted since day one (definitions block + allOf[user, base]).
/// </summary>
public static class SchemaUtility
{
    private const string DraftSchema = "http://json-schema.org/draft-04/schema#";
    private const string DefinitionsPrefix = "#/definitions/";

    private static readonly JsonSerializerOptions s_writeOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string GetJsonString(IFalloutBuild build)
    {
        // Trailing newline matches the legacy NJsonSchema output that consumer-side tooling expects.
        return BuildSchema(build).ToJsonString(s_writeOptions) + "\n";
    }

    public static JsonDocument GetJsonDocument(IFalloutBuild build)
    {
        return JsonDocument.Parse(GetJsonString(build));
    }

    private static JsonObject BuildSchema(IFalloutBuild build)
    {
        var ctx = new SchemaContext();

        // Pre-seed the "well-known" definitions in the legacy order: Host, ExecutableTarget, Verbosity, FalloutBuild.
        var hostNames = Host.AvailableTypes.Select(x => x.Name).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        ctx.Definitions["Host"] = StringEnumSchema(hostNames);

        var targetNames = ExecutableTargetFactory.GetTargetProperties(build.GetType())
            .Select(x => x.GetDisplayShortName())
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        ctx.Definitions["ExecutableTarget"] = targetNames.Count == 0
            ? new JsonObject { ["type"] = "string" }
            : StringEnumSchema(targetNames);

        ctx.Definitions["Verbosity"] = new JsonObject
        {
            ["type"] = "string",
            ["description"] = string.Empty,
            ["enum"] = new JsonArray("Verbose", "Normal", "Minimal", "Quiet")
        };

        // Build the FalloutBuild base schema (all framework-level parameters) and the user extension
        // schema (consumer-defined parameters). Parameters declared on FalloutBuild itself go into the
        // base; everything else into user.
        var baseSchema = new JsonObject();
        var baseProperties = new JsonObject();
        baseSchema["properties"] = baseProperties;

        var userSchema = new JsonObject();
        JsonObject userProperties = null;

        foreach (var member in ValueInjectionUtility.GetParameterMembers(build.GetType(), includeUnlisted: true))
        {
            var name = ParameterService.GetParameterMemberName(member);
            var property = BuildPropertySchema(member, build, ctx);
            if (member.DeclaringType == typeof(FalloutBuild))
            {
                baseProperties[name] = property;
            }
            else
            {
                userProperties ??= new JsonObject();
                userProperties[name] = property;
            }
        }

        if (userProperties != null)
            userSchema["properties"] = userProperties;

        // BuildProjectFile is read by the Fallout global tool's in-tool runner from .fallout/parameters.json
        // (see Fallout.Cli.BuildProjectResolver). It's not a [Parameter] on the build itself, but we
        // surface it in the schema so editors offer IntelliSense when consumers configure a non-conventional
        // build project path.
        baseProperties["BuildProjectFile"] = new JsonObject
        {
            ["type"] = new JsonArray("null", "string"),
            ["description"] = "Path to the build project (.csproj) relative to the repository root. Defaults to 'build/_build.csproj' when unset. Read by the Fallout global tool's in-tool runner."
        };

        // Force the framework parameters Skip/Target to reference the ExecutableTarget definition.
        if (baseProperties[InvokedTargetsParameterName] is JsonObject targetProp)
            targetProp["items"] = new JsonObject { ["$ref"] = DefinitionsPrefix + "ExecutableTarget" };
        if (baseProperties[SkippedTargetsParameterName] is JsonObject skipProp)
            skipProp["items"] = new JsonObject { ["$ref"] = DefinitionsPrefix + "ExecutableTarget" };
        ReplaceWithRef(baseProperties, nameof(FalloutBuild.Host), "Host");
        ReplaceWithRef(baseProperties, nameof(FalloutBuild.Verbosity), "Verbosity");

        ctx.Definitions[nameof(FalloutBuild)] = baseSchema;

        // Compose the final document in the legacy definition order: complex types first, then the
        // well-known ones, then FalloutBuild. Pre-seeded definitions get re-emitted in the order they
        // were added — JsonObject preserves insertion order — so we move complex types to the front.
        var orderedDefinitions = new JsonObject();
        foreach (var kvp in ctx.Definitions.Where(x => !IsWellKnownDefinition(x.Key)))
            orderedDefinitions[kvp.Key] = kvp.Value;
        foreach (var key in new[] { "Host", "ExecutableTarget", "Verbosity", nameof(FalloutBuild) })
        {
            if (ctx.Definitions.TryGetValue(key, out var value))
                orderedDefinitions[key] = value;
        }

        return new JsonObject
        {
            ["$schema"] = DraftSchema,
            ["definitions"] = orderedDefinitions,
            ["allOf"] = new JsonArray(
                userSchema,
                new JsonObject { ["$ref"] = DefinitionsPrefix + nameof(FalloutBuild) })
        };
    }

    private static JsonObject ReorderArraySchemaWithDescription(JsonObject schema, string description)
    {
        var ordered = new JsonObject();
        foreach (var (key, value) in schema.ToList())
        {
            schema.Remove(key);
            if (key == "items")
                ordered["description"] = description;
            ordered[key] = value;
        }
        return ordered;
    }

    private static void ReplaceWithRef(JsonObject properties, string propertyName, string definitionName)
    {
        if (properties[propertyName] is not JsonObject existing)
            return;
        var description = existing["description"]?.GetValue<string>();
        var replacement = new JsonObject();
        if (description != null)
            replacement["description"] = description;
        replacement["$ref"] = DefinitionsPrefix + definitionName;
        properties[propertyName] = replacement;
    }

    private static bool IsWellKnownDefinition(string name)
    {
        return name is "Host" or "ExecutableTarget" or "Verbosity" or nameof(FalloutBuild);
    }

    private static JsonObject StringEnumSchema(IEnumerable<string> values)
    {
        return new JsonObject
        {
            ["type"] = "string",
            ["enum"] = new JsonArray(values.Select(v => (JsonNode)v).ToArray())
        };
    }

    private static JsonObject BuildPropertySchema(MemberInfo member, IFalloutBuild build, SchemaContext ctx)
    {
        var memberType = member.GetMemberType();
        // Only the exact-typed [Parameter] attribute drives a recursive schema for the member's type.
        // Subclasses of ParameterAttribute (e.g. [CustomParameter]) render the member as plain "string".
        var attributeType = member.GetCustomAttribute<ParameterAttribute>().NotNull().GetType();
        var schema = attributeType == typeof(ParameterAttribute)
            ? SchemaForType(memberType, ctx)
            : new JsonObject { ["type"] = "string" };

        var description = ParameterService.GetParameterDescription(member);
        if (description != null)
        {
            // Match legacy layout: for array schemas, description sits between "type" and "items".
            // For everything else (string, $ref, etc.), description goes at the end.
            if (schema.ContainsKey("items"))
            {
                schema = ReorderArraySchemaWithDescription(schema, description);
            }
            else
            {
                schema["description"] = description;
            }
        }

        if (member.HasCustomAttribute<SecretAttribute>())
            schema["default"] = "Secrets must be entered via 'nuke :secrets [profile]'";

        // Override-with-enumeration: parameters with a value set become string enums (or array-of-string-enums).
        var valueSet = ParameterService.GetParameterValueSet(member, build)?.Select(x => x.Text).ToList();
        if (valueSet != null && !memberType.IsEnum)
        {
            var collection = memberType.IsCollectionLike();
            var enumSchema = collection
                ? new JsonObject { ["type"] = "array", ["items"] = StringEnumSchema(valueSet) }
                : StringEnumSchema(valueSet);
            // Preserve the description we just set.
            if (schema["description"] is JsonNode desc)
                enumSchema["description"] = desc.GetValue<string>();
            schema = enumSchema;
        }

        // Surface nullability for value types as [T, "null"] (matches the legacy NJsonSchema shape).
        if (memberType.IsValueType && Nullable.GetUnderlyingType(memberType) != null && schema["type"] is JsonValue tv)
        {
            schema["type"] = new JsonArray(tv.GetValue<string>(), "null");
        }

        return schema;
    }

    private static JsonObject SchemaForType(Type type, SchemaContext ctx)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(type);
        if (nullableUnderlying != null)
            return SchemaForType(nullableUnderlying, ctx);

        if (type == typeof(bool)) return new JsonObject { ["type"] = "boolean" };
        if (type == typeof(string)) return new JsonObject { ["type"] = "string" };
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            return new JsonObject { ["type"] = "integer", ["format"] = type == typeof(long) ? "int64" : "int32" };
        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            return new JsonObject { ["type"] = "number" };

        // AbsolutePath, Solution, Project — anything with a string-roundtripping TypeConverter — renders as a plain string.
        if (HasStringTypeConverter(type))
            return new JsonObject { ["type"] = "string" };

        if (typeof(Enumeration).IsAssignableFrom(type))
            return BuildEnumerationSchema(type);

        if (type.IsEnum)
            return StringEnumSchema(Enum.GetNames(type));

        if (type.IsArray)
            return new JsonObject { ["type"] = "array", ["items"] = SchemaForType(type.GetElementType()!, ctx) };

        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(IReadOnlyList<>) || def == typeof(IReadOnlyCollection<>) ||
                def == typeof(List<>) || def == typeof(IEnumerable<>))
            {
                return new JsonObject { ["type"] = "array", ["items"] = SchemaForType(type.GetGenericArguments()[0], ctx) };
            }
        }

        // Complex reference type — register a definition (if not already present) and emit a $ref.
        return BuildComplexTypeReference(type, ctx);
    }

    private static JsonObject BuildEnumerationSchema(Type enumerationType)
    {
        var values = enumerationType
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => enumerationType.IsAssignableFrom(f.FieldType))
            .Select(f => f.Name)
            .ToList();
        return StringEnumSchema(values);
    }

    private static JsonObject BuildComplexTypeReference(Type type, SchemaContext ctx)
    {
        var name = type.Name;
        if (!ctx.Definitions.ContainsKey(name))
        {
            // Insert a placeholder first so recursive types resolve via $ref.
            ctx.Definitions[name] = new JsonObject();
            var definition = BuildObjectSchema(type, ctx);
            // Replace placeholder with the populated schema.
            ctx.Definitions[name] = definition;
        }

        // For value-type-style complex shapes referenced as nullable, the original schema wrapped the
        // ref in `oneOf: [{"type": "null"}, {"$ref": ...}]`. Reference types appear directly via $ref.
        // The Nullable handling at the caller adds the null sibling if needed for value types; reference
        // types stay plain $ref.
        return new JsonObject { ["$ref"] = DefinitionsPrefix + name };
    }

    private static JsonObject BuildObjectSchema(Type type, SchemaContext ctx)
    {
        var properties = new JsonObject();
        foreach (var (memberName, memberType, _) in GetSerializableMembers(type))
        {
            properties[memberName] = WrapNullable(memberType, SchemaForType(memberType, ctx));
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties
        };
    }

    private static JsonObject WrapNullable(Type memberType, JsonObject schema)
    {
        // Convention matched to legacy NJsonSchema output:
        //   - Value-type Nullable<T>: ["T", "null"] (T first).
        //   - Reference type with $ref:  oneOf [{null}, {$ref}].
        //   - Reference type with primitive "string" / etc.: ["null", "T"] (null first).
        //   - Reference type with "array": ["array", "null"] (array first — quirk preserved).
        var nullableUnderlying = Nullable.GetUnderlyingType(memberType);
        if (nullableUnderlying != null && schema["type"] is JsonValue tv)
        {
            schema["type"] = new JsonArray(tv.GetValue<string>(), "null");
            return schema;
        }

        if (!memberType.IsValueType)
        {
            if (schema["$ref"] is JsonNode refNode)
            {
                return new JsonObject
                {
                    ["oneOf"] = new JsonArray(
                        new JsonObject { ["type"] = "null" },
                        new JsonObject { ["$ref"] = refNode.GetValue<string>() })
                };
            }
            if (schema["type"] is JsonValue typeValue)
            {
                var typeName = typeValue.GetValue<string>();
                if (typeName == "array")
                    schema["type"] = new JsonArray("array", "null");
                else if (typeName != "object")
                    schema["type"] = new JsonArray("null", typeName);
            }
        }

        return schema;
    }

    private static IEnumerable<(string Name, Type Type, MemberInfo Member)> GetSerializableMembers(Type type)
    {
        var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var field in type.GetFields(bindingFlags))
        {
            if (field.IsPrivate || field.IsStatic || field.IsInitOnly || field.IsLiteral)
                continue;
            yield return (field.Name, field.FieldType, field);
        }

        foreach (var property in type.GetProperties(bindingFlags))
        {
            if (property.GetMethod is null || property.GetMethod.IsPrivate || property.GetMethod.IsStatic)
                continue;
            yield return (property.Name, property.PropertyType, property);
        }
    }

    private static bool HasStringTypeConverter(Type type)
    {
        if (type == typeof(string)) return false;
        // The AbsolutePath / Solution / Project / Configuration story: any user-declared
        // [TypeConverter] that can deserialize from a string round-trips through a string in JSON.
        // (Most converters skip overriding CanConvertTo(string) because the base ToString fallback
        // is sufficient, so we don't enforce the symmetric check.)
        if (type.GetCustomAttribute<TypeConverterAttribute>(inherit: true) is null)
            return false;
        try
        {
            return TypeDescriptor.GetConverter(type)?.CanConvertFrom(typeof(string)) ?? false;
        }
        catch
        {
            return false;
        }
    }

    private class SchemaContext
    {
        public Dictionary<string, JsonObject> Definitions { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
