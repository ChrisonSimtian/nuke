using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Net;

namespace Fallout.Common.CI.GitHubActions;

public partial class GitHubActions
{
    public async Task CreateComment(int issue, string text)
    {
        await httpClient.Value
            .CreateRequest(HttpMethod.Post, $"repos/{Repository}/issues/{issue}/comments")
            .WithJsonContent(new
            {
                body = text
            })
            .GetResponseAsync();
    }

    private JsonObject GetJobDetails(long runId)
    {
        var response = httpClient.Value
            .CreateRequest(HttpMethod.Get, $"repos/{Repository}/actions/runs/{runId}/jobs")
            .GetResponse()
            .AssertSuccessfulStatusCode();

        return response.GetBodyAsJsonObject().GetAwaiter().GetResult()
            .GetChildren("jobs")
            .Single(x => x.GetPropertyStringValue("name") == Job);
    }

    private long GetJobId()
    {
        return GetJobDetails(RunId).GetPropertyValue<long>("id");
    }
}
