using System.Reflection;
using Fallout.Common.CI.AppVeyor;
using Fallout.Common.CI.AzurePipelines;
using Fallout.Common.CI.TeamCity;
using Fallout.Common.Tooling;
using Fallout.Common.ValueInjection;

namespace Fallout.Common.Tools.MinVer;

/// <summary>
/// Injects an instance of <see cref="MinVer"/> based on the local repository.
/// </summary>
public class MinVerAttribute : ValueInjectionAttributeBase
{
    public string Framework { get; set; }

    public bool UpdateBuildNumber { get; set; }

    public override object GetValue(MemberInfo member, object instance)
    {
        var version = MinVerTasks.MinVer(s => s
                .SetFramework(Framework)
                .DisableProcessOutputLogging())
            .Result;

        if (UpdateBuildNumber)
        {
            AzurePipelines.Instance?.UpdateBuildNumber(version.Version);
            TeamCity.Instance?.SetBuildNumber(version.Version);
            AppVeyor.Instance?.UpdateBuildVersion(version.Version);
        }

        return version;
    }
}
