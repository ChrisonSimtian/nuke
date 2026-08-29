using System.Net.Http;
using System.Threading.Tasks;
using Fallout.Common.Utilities.Net;

namespace Fallout.Common.Gitter;
//
//[Headers("Accept: application/json")]
//public interface IGitterRestClient
//{
//    [Post("/rooms/{roomId}/chatMessages")]
//    Task SendMessage (string roomId, [Body(BodySerializationMethod.UrlEncoded)] [AliasAs("text")] string message);
//}

public static class GitterTasks
{
    private static readonly HttpClient client = new();

    public static void SendGitterMessage(string message, string roomId, string token)
    {
        SendGitterMessageAsync(message, roomId, token).Wait();
    }

    public static async Task SendGitterMessageAsync(string message, string roomId, string token)
    {
        var response = await client.CreateRequest(HttpMethod.Post, $"https://api.gitter.im/v1/rooms/{roomId}/chatMessages")
            .WithBearerAuthentication(token)
            .GetResponseAsync();

        response.AssertSuccessfulStatusCode();
    }
}
