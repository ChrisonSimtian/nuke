using System.IO;
using Fallout.Common.CI.TeamCity;

namespace Fallout.Common.Specs.CI;

public class TestTeamCityAttribute : TeamCityAttribute, ITestConfigurationGenerator
{
    public StreamWriter Stream { get; set; }

    protected override StreamWriter CreateStream()
    {
        return Stream;
    }
}
