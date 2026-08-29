using Fallout.Common.Utilities;

namespace Fallout.Common.CI.TeamCity.Configuration;

public class TeamCityVcsTrigger : TeamCityTrigger
{
    public string[] BranchFilters { get; set; }

    public string[] TriggerRules { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        using (writer.WriteBlock("vcs"))
        {
            writer.WriteArray("branchFilter", BranchFilters);
            writer.WriteArray("triggerRules", TriggerRules);
        }
    }
}
