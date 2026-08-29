using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Fallout.Common.Git;
using Fallout.Common.Tools.GitHub;
using Fallout.Common.Utilities;
using Fallout.Common.ValueInjection;
using NuGet.Versioning;

namespace Fallout.Common.Tooling;

public class LatestGitHubReleaseAttribute : ValueInjectionAttributeBase
{
    private readonly string identifier;

    public LatestGitHubReleaseAttribute(string identifier)
    {
        this.identifier = identifier;
    }

    public bool IncludePrerelease { get; set; }

    public bool UseTagName { get; set; }

    public string Pattern { get; set; } = @"v?(?<version>\d+\.\d+(?:\.\d+)?(?:\.\d+)?(?:-\w+)?)";

    public override object GetValue(MemberInfo member, object instance)
    {
        var repository = GitRepository.FromUrl($"https://github.com/{identifier}");
        var releases = GitHubTasks.GitHubClient.Repository.Release
            .GetAll(repository.GetGitHubOwner(), repository.GetGitHubName()).GetAwaiter().GetResult();

        var versions = releases
            .Select(x => Regex.Match((!UseTagName ? x.Name : x.TagName).NotNullOrWhiteSpace(), Pattern))
            .Select(x => x.Groups["version"].Value)
            .Select(NuGetVersion.Parse)
            .OrderByDescending(x => x);

        if (member.GetMemberType() == typeof(NuGetVersion[]))
        {
            return versions.ToArray();
        }

        var latestVersion = versions.FirstOrDefault(x => !x.IsPrerelease || IncludePrerelease);
        return member.GetMemberType() == typeof(NuGetVersion)
            ? latestVersion
            : latestVersion?.ToString();
    }
}
