using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;

namespace Fallout.Common.CI.TeamCity;

public static class DotNetTestSettingsExtensions
{
    public static DotNetTestSettings AddTeamCityLogger(this DotNetTestSettings toolSettings)
    {
        Assert.True(TeamCity.Instance != null);
        var teamcityPackage = NuGetPackageResolver
            .GetLocalInstalledPackage("TeamCity.Dotnet.Integration", NuGetToolPathResolver.NuGetPackagesConfigFile)
            .NotNull("teamcityPackage != null");

        var loggerPath = teamcityPackage.Directory / "build" / "_common" / "vstest15";
        Assert.DirectoryExists(loggerPath);
        return toolSettings
            .SetLoggers("teamcity")
            .SetTestAdapterPath(loggerPath);
    }
}
