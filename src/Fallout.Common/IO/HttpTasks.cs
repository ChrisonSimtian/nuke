using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Fallout.Common.Tooling;

namespace Fallout.Common.IO;

public static class HttpTasks
{
    public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public static string HttpDownloadString(
        string uri,
        Configure<HttpClient> clientConfigurator = null,
        Action<HttpRequestHeaders> headerConfigurator = null)
    {
        return HttpDownloadStringAsync(uri, clientConfigurator, headerConfigurator).GetAwaiter().GetResult();
    }

    public static async Task<string> HttpDownloadStringAsync(
        string uri,
        Configure<HttpClient> clientConfigurator = null,
        Action<HttpRequestHeaders> headerConfigurator = null)
    {
        var httpClient = CreateHttpClient(clientConfigurator, headerConfigurator);
        return await httpClient.GetAsync(uri).Result.Content.ReadAsStringAsync();
    }

    public static void HttpDownloadFile(
        string uri,
        string path,
        FileMode mode = FileMode.Create,
        Configure<HttpClient> clientConfigurator = null,
        Action<HttpRequestHeaders> headerConfigurator = null)
    {
        HttpDownloadFileAsync(uri, path, mode, clientConfigurator, headerConfigurator).GetAwaiter().GetResult();
    }

    public static async Task HttpDownloadFileAsync(
        string uri,
        AbsolutePath path,
        FileMode mode = FileMode.Create,
        Configure<HttpClient> clientConfigurator = null,
        Action<HttpRequestHeaders> headerConfigurator = null)
    {
        var httpClient = CreateHttpClient(clientConfigurator, headerConfigurator);
        var response = await httpClient.GetAsync(uri);
        Assert.True(response.IsSuccessStatusCode, $"{response.ReasonPhrase}: {uri}");

        path.Parent.CreateDirectory();
        await using var fileStream = File.Open(path, mode);
        await response.Content.CopyToAsync(fileStream);
    }

    private static HttpClient CreateHttpClient(
        Configure<HttpClient> clientConfigurator = null,
        Action<HttpRequestHeaders> headerConfigurator = null)
    {
        var httpClient = new HttpClient
        {
            Timeout = DefaultTimeout
        };

        clientConfigurator?.Invoke(httpClient);
        headerConfigurator?.Invoke(httpClient.DefaultRequestHeaders);
        return httpClient;
    }
}
