using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;

namespace Fallout.Common.CI.TeamCity;

public static class DotNetBuildSettingsExtensions
{
    public static DotNetBuildSettings AddTeamCityLogger(this DotNetBuildSettings toolSettings)
    {
        Assert.True(TeamCity.Instance != null);
        var teamcityPackage = NuGetPackageResolver
            .GetLocalInstalledPackage("TeamCity.Dotnet.Integration", NuGetToolPathResolver.NuGetPackagesConfigFile)
            .NotNull("teamcityPackage != null");

        var loggerAssembly = teamcityPackage.Directory / "build" / "_common" / "msbuild15" / "TeamCity.MSBuild.Logger.dll";
        Assert.FileExists(loggerAssembly);
        return toolSettings
            .AddLoggers($"TeamCity.MSBuild.Logger.TeamCityMSBuildLogger,{loggerAssembly};teamcity")
            .EnableNoConsoleLogger();
    }
}
