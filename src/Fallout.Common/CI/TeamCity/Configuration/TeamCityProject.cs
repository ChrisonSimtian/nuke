using System.Linq;
using Fallout.Common.Utilities;

namespace Fallout.Common.CI.TeamCity.Configuration;

public class TeamCityProject : ConfigurationEntity
{
    public string Description { get; set; }

    public TeamCityParameter[] Parameters { get; set; }

    public TeamCityVcsRoot VcsRoot { get; set; }

    public TeamCityBuildType[] BuildTypes { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        using (writer.WriteBlock("project"))
        {
            if (Description != null)
            {
                writer.WriteLine($"description = {Description}");
            }

            foreach (var buildType in BuildTypes)
            {
                writer.WriteLine($"buildType({buildType.Id})");
            }

            writer.WriteLine();

            writer.WriteLine($"buildTypesOrder = arrayListOf({BuildTypes.Select(x => x.Id).JoinCommaSpace()})");
            writer.WriteLine();

            if (Parameters.Any())
            {
                using (writer.WriteBlock("params"))
                {
                    foreach (var parameter in Parameters)
                    {
                        parameter.Write(writer);
                    }
                }
            }
        }
    }
}
