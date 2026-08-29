using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.Tools.Twitter;

public static class TwitterTasks
{
    // Based on...
    // https://blog.dantup.com/2016/07/simplest-csharp-code-to-post-a-tweet-using-oauth/
    // https://www.thatsoftwaredude.com/content/6289/how-to-post-a-tweet-using-c-for-single-user

    private const string Url = "https://api.twitter.com/1.1/statuses/update.json";

    public static void SendTweet(
        string message,
        string consumerKey,
        string consumerSecret,
        string accessToken,
        string accessTokenSecret)
    {
        SendTweetAsync(message, consumerKey, consumerSecret, accessToken, accessTokenSecret).Wait();
    }

    public static async Task SendTweetAsync(
        string message,
        string consumerKey,
        string consumerSecret,
        string accessToken,
        string accessTokenSecret)
    {
        var timestamp = ConvertToUnixTimestamp(DateTime.Now).ToString(CultureInfo.InvariantCulture);
        var data =
            new Dictionary<string, string>
            {
                { "status", message },
                { "trim_user", "1" },
                { "oauth_consumer_key", consumerKey },
                { "oauth_nonce", Convert.ToBase64String(Encoding.UTF8.GetBytes(timestamp)) },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", timestamp },
                { "oauth_token", accessToken },
                { "oauth_version", "1.0" }
            };

        data.AddPair("oauth_signature", GetOAuthSignature(data, Url, consumerSecret, accessTokenSecret));

        var authorization = GetOAuthHeader(data);
        var formData = new FormUrlEncodedContent(data.Where(kvp => !kvp.Key.StartsWith("oauth_")));

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", authorization);

        var response = await client.PostAsync(Url, formData);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"StatusCode != 200 - '{GetErrorFromBody(responseBody)}'");
    }

    private static string GetOAuthSignature(Dictionary<string, string> data, string url, string consumerSecret,
        string tokenSecret)
    {
        var signature = new[]
            {
                "POST",
                Uri.EscapeDataString(url)
            }
            .Concat(Uri.EscapeDataString(data
                .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}")
                .OrderBy(x => x)
                .JoinAmpersand()))
            .JoinAmpersand();

        var cryptoKey = Encoding.ASCII.GetBytes(new[]
        {
            consumerSecret,
            tokenSecret
        }.JoinAmpersand());

        var cryptoTransform = new HMACSHA1(cryptoKey);
        return Convert.ToBase64String(cryptoTransform.ComputeHash(Encoding.ASCII.GetBytes(signature)));
    }

    private static string GetOAuthHeader(Dictionary<string, string> data)
    {
        return string.Format("OAuth {0}",
            data.Where(x => x.Key.StartsWith("oauth_"))
                .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value).DoubleQuote()}")
                .JoinCommaSpace());
    }

    private static string GetErrorFromBody(string response)
    {
        try
        {
            var jResponse = JsonNode.Parse(response).NotNull().AsObject();
            var message = jResponse["errors"].NotNull().AsArray()[0].NotNull()["message"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(message))
            {
                return message;
            }
        }
        catch
        {
            // ignored
        }

        return response;
    }

    private static double ConvertToUnixTimestamp(DateTime date)
    {
        var origin = new DateTime(year: 1970, month: 1, day: 1, hour: 0, minute: 0, second: 0, millisecond: 0);
        var diff = date.ToUniversalTime() - origin;
        return Math.Floor(diff.TotalSeconds);
    }
}
