using System.Linq;
using System.Reflection;
using Fallout.Common.CI.AppVeyor;
using Fallout.Common.CI.AzurePipelines;
using Fallout.Common.CI.TeamCity;
using Fallout.Common.Git;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Fallout.Common.ValueInjection;
using Serilog;
using static Fallout.Common.ControlFlow;

namespace Fallout.Common.Tools.GitVersion;

/// <summary>
/// Injects an instance of <see cref="GitVersion"/> based on the local repository.
/// </summary>
public class GitVersionAttribute : ValueInjectionAttributeBase
{
    public string Framework { get; set; }

    public bool DisableOnUnix { get; set; }

    public bool UpdateAssemblyInfo { get; set; }

    public bool UpdateBuildNumber { get; set; } = true;

    public bool NoFetch { get; set; }

    public bool NoCache { get; set; } = true;

    public override object GetValue(MemberInfo member, object instance)
    {
        // TODO: https://github.com/GitTools/GitVersion/issues/1097
        if (EnvironmentInfo.IsUnix && DisableOnUnix)
        {
            Log.Warning("{Tool} is disabled on UNIX environment", nameof(GitVersion));
            return null;
        }

        var repository = SuppressErrors(() => GitRepository.FromLocalDirectory(Build.RootDirectory));
        if (repository is { Protocol: GitProtocol.Ssh } && !NoFetch)
        {
            Log.Warning(
                $"{nameof(GitVersion)} does not support fetching SSH endpoints, enable {nameof(NoFetch)} to skip fetching");
        }

        var gitVersion = GitVersionTasks.GitVersion(s => s
                .SetFramework(Framework)
                .SetNoFetch(NoFetch)
                .SetNoCache(NoCache)
                .DisableProcessOutputLogging()
                .SetUpdateAssemblyInfo(UpdateAssemblyInfo)
                .When(TeamCity.Instance is { IsPullRequest: true } && !EnvironmentInfo.Variables.ContainsKey("Git_Branch"), _ => _
                    .AddProcessEnvironmentVariable(
                        "Git_Branch",
                        TeamCity.Instance.ConfigurationProperties.Single(x => x.Key.StartsWith("teamcity.build.vcs.branch"))
                            .Value)))
            .Result;

        if (UpdateBuildNumber)
        {
            AzurePipelines.Instance?.UpdateBuildNumber(gitVersion.FullSemVer);
            TeamCity.Instance?.SetBuildNumber(gitVersion.FullSemVer);
            AppVeyor.Instance?.UpdateBuildVersion($"{gitVersion.FullSemVer}.build.{AppVeyor.Instance.BuildNumber}");
        }

        return gitVersion;
    }
}
