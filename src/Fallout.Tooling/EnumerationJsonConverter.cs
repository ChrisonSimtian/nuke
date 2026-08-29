using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fallout.Common.Tooling;

/// <summary>
/// Bridges <see cref="Enumeration"/> subclasses across the System.Text.Json boundary.
/// Newtonsoft would honour the [TypeConverter] attribute for string round-trip serialization;
/// STJ doesn't, so this factory routes (de)serialization through the type's <see cref="TypeConverter"/>.
/// </summary>
internal class EnumerationJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(Enumeration).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(TypedConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType).NotNull();
    }

    private class TypedConverter<T> : JsonConverter<T>
        where T : Enumeration
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            var raw = reader.GetString();
            var converter = TypeDescriptor.GetConverter(typeToConvert);
            return (T)converter.ConvertFromInvariantString(raw);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue((string)value);
            }
        }

        public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var raw = reader.GetString();
            var converter = TypeDescriptor.GetConverter(typeToConvert);
            return (T)converter.ConvertFromInvariantString(raw);
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WritePropertyName((string)value);
        }
    }
}
