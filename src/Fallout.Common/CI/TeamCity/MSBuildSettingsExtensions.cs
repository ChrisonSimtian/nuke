using Fallout.Common.Tools.MSBuild;

namespace Fallout.Common.CI.TeamCity;

public static class MSBuildSettingsExtensions
{
    public static MSBuildSettings AddTeamCityLogger(this MSBuildSettings toolSettings)
    {
        var teamCity = TeamCity.Instance.NotNull("TeamCity.Instance != null");
        var teamCityLogger = teamCity.ConfigurationProperties["teamcity.dotnet.msbuild.extensions4.0"];
        return toolSettings
            .AddLoggers($"JetBrains.BuildServer.MSBuildLoggers.MSBuildLogger,{teamCityLogger}")
            .EnableNoConsoleLogger();
    }
}
