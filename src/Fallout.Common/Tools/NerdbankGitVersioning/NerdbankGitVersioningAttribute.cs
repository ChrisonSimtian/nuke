using System.Reflection;
using Fallout.Common.CI.AppVeyor;
using Fallout.Common.CI.AzurePipelines;
using Fallout.Common.CI.TeamCity;
using Fallout.Common.Tooling;
using Fallout.Common.ValueInjection;

namespace Fallout.Common.Tools.NerdbankGitVersioning;

/// <summary>
/// Injects an instance of <see cref="NerdbankGitVersioning"/> based on the local repository.
/// </summary>
public class NerdbankGitVersioningAttribute : ValueInjectionAttributeBase
{
    public bool UpdateBuildNumber { get; set; }

    public override object GetValue(MemberInfo member, object instance)
    {
        var version = NerdbankGitVersioningTasks.NerdbankGitVersioningGetVersion(s => s
                .DisableProcessOutputLogging()
                .SetFormat(NerdbankGitVersioningFormat.json))
            .Result;

        if (UpdateBuildNumber)
        {
            AzurePipelines.Instance?.UpdateBuildNumber(version.SemVer2);
            TeamCity.Instance?.SetBuildNumber(version.SemVer2);
            AppVeyor.Instance?.UpdateBuildVersion($"{version.SemVer2}.build.{AppVeyor.Instance.BuildNumber}");
        }

        return version;
    }
}
