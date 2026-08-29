using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Fallout.Common.Utilities.Net;

public static partial class HttpRequestExtensions
{
    /// <summary>
    /// Sets the JSON-serialized object as content via <see cref="JsonSerializer.Serialize{TValue}(TValue, JsonSerializerOptions?)"/>.
    /// </summary>
    public static HttpRequestBuilder WithJsonContent<T>(this HttpRequestBuilder builder, T obj)
    {
        var content = JsonSerializer.Serialize(obj);
        return builder.WithStringContent(content, "application/json");
    }

    /// <summary>
    /// Sets the JSON-serialized object as content via <see cref="JsonSerializer.Serialize{TValue}(TValue, JsonSerializerOptions?)"/>.
    /// </summary>
    public static HttpRequestBuilder WithJsonContent<T>(this HttpRequestBuilder builder, T obj, JsonSerializerOptions options)
    {
        var content = JsonSerializer.Serialize(obj, options);
        return builder.WithStringContent(content, "application/json");
    }

    /// <summary>
    /// Sets the string as content.
    /// </summary>
    public static HttpRequestBuilder WithStringContent(this HttpRequestBuilder builder, string content, string mediaType)
    {
        builder.Request.Content = new StringContent(content, Encoding.UTF8, mediaType);
        return builder;
    }

    /// <summary>
    /// Sets the dictionary as content via <see cref="FormUrlEncodedContent"/>.
    /// </summary>
    public static HttpRequestBuilder WithFormUrlEncodedContent(this HttpRequestBuilder builder,
        IDictionary<string, string> dictionary)
    {
        builder.Request.Content = new FormUrlEncodedContent(dictionary);
        return builder;
    }

    /// <summary>
    /// Sets a <see cref="MultipartFormDataContent"/> as content.
    /// </summary>
    public static HttpRequestBuilder WithMultipartFormDataContent(
        this HttpRequestBuilder builder,
        Func<MultipartFormDataContent, MultipartFormDataContent> configurator)
    {
        builder.Request.Content = configurator.Invoke(new MultipartFormDataContent());
        return builder;
    }

    /// <summary>
    /// Adds a string as content to a <see cref="MultipartFormDataContent"/>.
    /// </summary>
    public static MultipartFormDataContent AddStringContent(this MultipartFormDataContent data, string name, string content)
    {
        data.Add(new StringContent(content), name);
        return data;
    }

    /// <summary>
    /// Adds a stream as content to a <see cref="MultipartFormDataContent"/>.
    /// </summary>
    public static MultipartFormDataContent AddStreamContent(
        this MultipartFormDataContent data,
        string name,
        Stream content,
        string filename,
        string mediaType = null)
    {
        var streamContent = new StreamContent(content);
        if (mediaType != null)
        {
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        }

        data.Add(streamContent, name, filename);
        return data;
    }
}
