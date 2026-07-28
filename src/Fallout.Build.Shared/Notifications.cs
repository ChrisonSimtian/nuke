using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;

namespace Fallout.Build.Shared;

internal record Notification(string Title, string Text, Link[] Links)
{
    public string Title { get; } = Title;
    public string Text { get; } = Text;
    public Link[] Links { get; } = Links;
}

internal record Link(string Text, string Url)
{
    public string Text { get; } = Text;
    public string Url { get; } = Url;
}

internal class NotificationFetcher
{
    // TODO: host this file. Until then GetNotificationAsync catches the 404 and returns null.
    private const string NotificationEndpoint = Constants.FalloutNotificationsUrl;
    private const string UtmMedium = "development";

    private readonly AbsolutePath notificationDirectory = FalloutPaths.GlobalFalloutDirectory / "received-notifications";
    private readonly string utmSource;

    public NotificationFetcher(string utmSource)
    {
        this.utmSource = utmSource;
    }

    public async Task<Notification> GetNotificationAsync()
    {
        try
        {
            return await GetNotificationInternal();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<Notification> GetNotificationInternal()
    {
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(NotificationEndpoint);
        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content).RootElement;

        var notification = json.EnumerateArray()
            .Where(IsApplicable)
            .Select(x => (Json: x, File: notificationDirectory / x.ToString().GetMD5Hash()))
            .FirstOrDefault(x => !x.File.Exists());
        if (notification.File == null)
            return null;

        notification.File.TouchFile();

        return new Notification(
            Title: notification.Json.GetProperty("title").GetString(),
            Text: notification.Json.GetProperty("text").GetString(),
            Links: notification.Json.GetProperty("links").EnumerateArray().Select(GetLink).ToArray());

        bool IsApplicable(JsonElement element)
            => !element.TryGetProperty("exclude", out var exclusions) ||
               !exclusions.EnumerateArray().Select(x => x.GetString()).Contains(utmSource);

        Link GetLink(JsonElement obj)
        {
            var originalUrl = new Uri(obj.GetProperty("url").GetString().NotNullOrEmpty())
                .WithUtmValues(
                    medium: UtmMedium,
                    source: utmSource,
                    campaign: obj.GetProperty("campaign").GetString());
            return new Link(
                Text: obj.GetProperty("title").GetString(),
                Url: originalUrl.AbsoluteUri);
        }
    }
}
