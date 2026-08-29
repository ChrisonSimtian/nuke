using Fallout.Common.Utilities;

namespace Fallout.Common.CI.TeamCity.Configuration;

public class TeamCitySnapshotDependency : TeamCityDependency
{
    public TeamCityBuildType BuildType { get; set; }

    public TeamCityDependencyFailureAction FailureAction { get; set; }

    public TeamCityDependencyFailureAction CancelAction { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        static string FormatAction(TeamCityDependencyFailureAction action)
            => "FailureAction." +
               action.ToString().SplitCamelHumps().JoinUnderscore().ToUpperInvariant();

        using (writer.WriteBlock($"snapshot({BuildType.Id})"))
        {
            writer.WriteLine($"onDependencyFailure = {FormatAction(FailureAction)}");
            writer.WriteLine($"onDependencyCancel = {FormatAction(CancelAction)}");
        }
    }
}
