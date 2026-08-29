using Fallout.Common.Utilities;

namespace Fallout.Common.CI.GitHubActions.Configuration;

public class GitHubActionsScheduledTrigger : GitHubActionsDetailedTrigger
{
    public string Cron { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("schedule:");
        using (writer.Indent())
        {
            writer.WriteLine($"- cron: '{Cron}'");
        }
    }
}
