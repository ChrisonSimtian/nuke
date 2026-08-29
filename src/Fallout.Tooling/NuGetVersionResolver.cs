using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Fallout.Common.Tooling;

public static class NuGetVersionResolver
{
    private static readonly HttpClient client = new();

    public static async Task<string> GetLatestVersion(string packageId, bool includePrereleases, bool includeUnlisted = false)
    {
        try
        {
            var url = includeUnlisted
                ? $"https://api.nuget.org/v3/flatcontainer/{packageId.ToLowerInvariant()}/index.json"
                : $"https://api-v2v3search-0.nuget.org/query?q=packageid:{packageId}&prerelease={includePrereleases}";

            var jsonString = await client.GetStringAsync(url);
            var jsonObject = JsonNode.Parse(jsonString)?.AsObject().NotNull();

            if (includeUnlisted)
            {
                var versions = jsonObject!.First().Value.NotNull().AsArray()
                    .Select(x => x!.GetValue<string>());

                return versions.Last(x => includePrereleases || !x.Contains("-"));
            }

            return jsonObject!["data"].NotNull().AsArray().Single()!["version"].NotNull().ToString();
        }
        catch
        {
            return null;
        }
    }
}
