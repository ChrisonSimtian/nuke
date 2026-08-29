using Fallout.Common.Utilities;

namespace Fallout.Common.CI.TeamCity.Configuration;

public class TeamCityVcsRoot : ConfigurationEntity
{
    public string Id => "DslContext.settingsRoot";

    public override void Write(CustomFileWriter writer)
    {
    }
}
