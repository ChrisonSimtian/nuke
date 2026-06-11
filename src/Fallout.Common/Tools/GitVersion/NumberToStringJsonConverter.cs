// Copyright 2026 Maintainers of Fallout.
// Originally based on NUKE by Matthias Koch and contributors.
// Distributed under the MIT License.
// https://github.com/Fallout-build/Fallout/blob/main/LICENSE

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fallout.Common.Tools.GitVersion;

/// <summary>
/// Deserializes a JSON string <em>or</em> JSON number as a C# <see langword="string"/>.
/// Useful for tool outputs (e.g. GitVersion 6.x) where a field that was previously
/// a quoted string is now emitted as a bare number.
/// </summary>
internal sealed class NumberToStringJsonConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var l) ? l.ToString() : reader.GetDouble().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Unexpected token type {reader.TokenType} when deserializing string.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}
