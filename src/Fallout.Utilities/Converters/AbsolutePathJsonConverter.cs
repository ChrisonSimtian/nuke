#if NET6_0_OR_GREATER
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fallout.Common;
using Fallout.Common.IO;
using static Fallout.Common.IO.PathConstruction;

namespace Fallout.Utilities.Converters;

public sealed class AbsolutePathJsonConverter : JsonConverter<AbsolutePath>
{
    public override AbsolutePath Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value != null)
        {
            return HasPathRoot(value)
                ? AbsolutePath.Create(value)
                : EnvironmentInfo.WorkingDirectory / value;
        }

        return null;
    }

    public override void Write(
        Utf8JsonWriter writer,
        AbsolutePath value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
#endif
