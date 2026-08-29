using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fallout.Common.IO;

namespace Fallout.Common.Utilities.Net;

public static partial class HttpResponseExtensions
{
    /// <summary>
    /// Reads the HTTP response body as JSON via <see cref="JsonSerializer.Deserialize{TValue}(string, JsonSerializerOptions?)"/>.
    /// </summary>
    public static async Task<T> GetBodyAsJson<T>(this HttpResponseInspector inspector)
    {
        return JsonSerializer.Deserialize<T>(await inspector.GetBodyAsync());
    }

    /// <summary>
    /// Reads the HTTP response body as JSON via <see cref="JsonSerializer.Deserialize{TValue}(string, JsonSerializerOptions?)"/>.
    /// </summary>
    public static async Task<T> GetBodyAsJson<T>(this HttpResponseInspector inspector, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(await inspector.GetBodyAsync(), options);
    }

    /// <summary>
    /// Reads the HTTP response body as a <see cref="JsonObject"/>.
    /// </summary>
    public static async Task<JsonObject> GetBodyAsJsonObject(this HttpResponseInspector inspector)
    {
        return JsonNode.Parse(await inspector.GetBodyAsync())?.AsObject();
    }

    /// <summary>
    /// Reads the HTTP response body as a <see cref="JsonObject"/> using the given <paramref name="options"/>.
    /// </summary>
    public static async Task<JsonObject> GetBodyAsJsonObject(this HttpResponseInspector inspector, JsonNodeOptions options)
    {
        return JsonNode.Parse(await inspector.GetBodyAsync(), nodeOptions: options)?.AsObject();
    }

    public static async Task WriteToFile(this HttpResponseInspector inspector, AbsolutePath path,
        FileMode mode = FileMode.CreateNew)
    {
        using var fileStream = File.Open(path, mode);
        await inspector.Response.Content.CopyToAsync(fileStream);
    }
}
