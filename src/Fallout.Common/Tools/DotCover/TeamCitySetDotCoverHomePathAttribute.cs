using System.Collections.Generic;
using Fallout.Common.CI.TeamCity;
using Fallout.Common.Execution;

namespace Fallout.Common.Tools.DotCover;

public class TeamCitySetDotCoverHomePathAttribute : BuildExtensionAttributeBase, IOnBuildInitialized
{
    public void OnBuildInitialized(
        IReadOnlyCollection<ExecutableTarget> executableTargets,
        IReadOnlyCollection<ExecutableTarget> executionPlan)
    {
        TeamCity.Instance?.SetConfigurationParameter("teamcity.dotCover.home", DotCoverTasks.DotCoverPath);
    }
}
