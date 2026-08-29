using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Fallout.CodeGeneration.Model;
using Fallout.Common.IO;
using Serilog;

namespace Fallout.CodeGeneration;

public static class ToolSerializer
{
    private static readonly JsonSerializerOptions readOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions writeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        // Match Newtonsoft's default — leave <, >, ' etc. unescaped in the output file.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                ApplySerializationFilters
            }
        }
    };

    public static Tool Load(string file)
    {
        try
        {
            var content = File.ReadAllText(file);
            return JsonSerializer.Deserialize<Tool>(content, readOptions);
        }
        catch (Exception exception)
        {
            // TODO: update metadata -> specification
            Log.Error(exception, "Couldn't load metadata file {File}", Path.GetFileName(file));
            throw;
        }
    }

    public static void Save(Tool tool, AbsolutePath file)
    {
        file.WriteAllText(JsonSerializer.Serialize(tool, writeOptions));
    }

    // Mirrors the legacy Newtonsoft CustomContractResolver. Two-pass shape:
    //   1. Strip read-only properties (no setter, or setter inherited while getter is overridden).
    //      STJ calls the getter BEFORE invoking ShouldSerialize, so a filter alone can't prevent
    //      throws on uninitialized computed properties like SettingsClass.Name.
    //   2. Per-property ShouldSerialize for cases that aren't covered by DefaultIgnoreCondition:
    //      empty enumerables, and SettingsClass.ExtensionMethods (which defaults to true, not false).
    //   Value-type defaults and null reference values are handled by DefaultIgnoreCondition.WhenWritingDefault.
    private static void ApplySerializationFilters(JsonTypeInfo typeInfo)
    {
        var readOnlyProperties = new List<JsonPropertyInfo>();
        foreach (var jsonProperty in typeInfo.Properties)
        {
            if (jsonProperty.AttributeProvider is not PropertyInfo declared)
            {
                continue;
            }

            // Tool.Schema is intentionally read-only but must serialize.
            if (typeInfo.Type == typeof(Tool) && declared.Name == nameof(Tool.Schema))
            {
                continue;
            }

            var getter = declared.GetGetMethod(nonPublic: true);
            var setter = declared.GetSetMethod(nonPublic: true);
            if (setter == null)
            {
                readOnlyProperties.Add(jsonProperty);
            }
            else if (getter != null && getter.DeclaringType != setter.DeclaringType)
            {
                // Override-only-getter pattern: SettingsClass.Name overrides DataClass.Name without
                // a setter. The PropertyInfo carries the inherited base setter; the only safe signal
                // is the mismatched declaring types.
                readOnlyProperties.Add(jsonProperty);
            }
        }

        foreach (var p in readOnlyProperties)
        {
            typeInfo.Properties.Remove(p);
        }

        foreach (var jsonProperty in typeInfo.Properties)
        {
            if (jsonProperty.AttributeProvider is not PropertyInfo declared)
            {
                continue;
            }

            var propertyName = declared.Name;
            var propertyType = declared.PropertyType;

            // ExtensionMethods defaults to false on DataClass but true on SettingsClass (constructor sets it).
            // On the DataClass typeInfo, DefaultIgnoreCondition.WhenWritingDefault skips false correctly,
            // so leave ShouldSerialize unset. On SettingsClass typeInfo, override to skip the true default.
            if (propertyType == typeof(bool) && propertyName == nameof(DataClass.ExtensionMethods) &&
                typeInfo.Type == typeof(SettingsClass))
            {
                jsonProperty.ShouldSerialize = (_, value) => value is not true;
                continue;
            }

            if (propertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(propertyType))
            {
                jsonProperty.ShouldSerialize = (_, value) =>
                {
                    if (value is not IEnumerable enumerable)
                    {
                        return false;
                    }

                    var enumerator = enumerable.GetEnumerator();
                    try { return enumerator.MoveNext(); }
                    finally { (enumerator as IDisposable)?.Dispose(); }
                };
            }
        }
    }
}
