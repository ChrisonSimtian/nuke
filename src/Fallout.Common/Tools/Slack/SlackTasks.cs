using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Net;

namespace Fallout.Common.Tools.Slack;

public static class SlackTasks
{
    private static readonly HttpClient client = new();

    public static void SendSlackMessage(Configure<SlackMessage> configurator, string webhook)
    {
        SendSlackMessageAsync(configurator, webhook).Wait();
    }

    public static async Task SendSlackMessageAsync(Configure<SlackMessage> configurator, string webhook)
    {
        var message = configurator(new SlackMessage());
        var payload = JsonSerializer.Serialize(message);

        var response = await client.CreateRequest(HttpMethod.Post, webhook)
            .WithFormUrlEncodedContent(new Dictionary<string, string>
            {
                ["payload"] = payload
            })
            .GetResponseAsync();

        var responseText = await response.GetBodyAsync();
        Assert.True(responseText == "ok");
    }

    public static async Task<string> SendOrUpdateSlackMessage(Configure<SlackMessage> configurator, string accessToken)
    {
        var message = configurator(new SlackMessage());

        var response = await client.CreateRequest(
                HttpMethod.Post,
                message.Timestamp == null
                    ? "https://slack.com/api/chat.postMessage"
                    : "https://slack.com/api/chat.update")
            .WithBearerAuthentication(accessToken)
            .WithJsonContent(message)
            .GetResponseAsync();

        var jobject = await response.GetBodyAsJsonObject();
        var error = jobject.GetPropertyValueOrNull<string>("error");
        Assert.True(error == null, error);

        return jobject.GetPropertyStringValue("ts");
    }
}

[ExcludeFromCodeCoverage]
[Serializable]
public class SlackMessageActionButton : SlackMessageAction
{
    [JsonPropertyName("type")]
    public string Type => "button";
}
