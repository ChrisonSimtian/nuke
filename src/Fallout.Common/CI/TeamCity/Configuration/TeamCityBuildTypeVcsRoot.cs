using Fallout.Common.Utilities;

namespace Fallout.Common.CI.TeamCity.Configuration;

public class TeamCityBuildTypeVcsRoot : ConfigurationEntity
{
    public TeamCityVcsRoot Root { get; set; }

    public bool ShowDependenciesChanges { get; set; }

    public bool CleanCheckoutDirectory { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        using (writer.WriteBlock("vcs"))
        {
            writer.WriteLine($"root({Root.Id})");
            if (CleanCheckoutDirectory)
            {
                writer.WriteLine("cleanCheckout = true");
            }

            if (ShowDependenciesChanges)
            {
                writer.WriteLine("showDependenciesChanges = true");
            }
        }
    }
}
