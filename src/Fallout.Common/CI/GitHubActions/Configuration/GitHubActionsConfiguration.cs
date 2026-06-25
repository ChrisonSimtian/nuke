using System.Linq;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI.GitHubActions.Configuration;

public class GitHubActionsConfiguration : ConfigurationEntity
{
    public string Name { get; set; }

    public GitHubActionsTrigger[] ShortTriggers { get; set; }
    public GitHubActionsDetailedTrigger[] DetailedTriggers { get; set; }
    public string[] Env { get; set; } = new string[0];
    public (GitHubActionsPermissions Type, string Permission)[] Permissions { get; set; }
    public string ConcurrencyGroup { get; set; }
    public bool ConcurrencyCancelInProgress { get; set; }
    public string DefaultShell { get; set; }
    public GitHubActionsJob[] Jobs { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine($"name: {Name}");
        writer.WriteLine();

        if (ShortTriggers.Length > 0)
            writer.WriteLine($"on: [{ShortTriggers.Select(x => x.GetValue().ToLowerInvariant()).JoinCommaSpace()}]");
        else
        {
            writer.WriteLine("on:");
            using (writer.Indent())
            {
                DetailedTriggers.ForEach(x => x.Write(writer));
            }
        }

        if (Env.Length > 0)
        {
            writer.WriteLine();
            writer.WriteLine("env:");
            using (writer.Indent())
            {
                Env.ForEach(x => writer.WriteLine(x));
            }
        }

        if (Permissions.Length > 0)
        {
            writer.WriteLine();
            writer.WriteLine("permissions:");
            using (writer.Indent())
            {
                Permissions.ForEach(x => writer.WriteLine($"{x.Type.GetValue()}: {x.Permission}"));
            }
        }

        if (!ConcurrencyGroup.IsNullOrWhiteSpace() || ConcurrencyCancelInProgress)
        {
            writer.WriteLine();
            writer.WriteLine("concurrency:");
            using (writer.Indent())
            {
                var group = ConcurrencyGroup;
                if (group.IsNullOrWhiteSpace())
                {
                    // create a default value that only cancels in-progress runs of the same workflow
                    // we don't fall back to github.ref which would disable multiple runs in main/master which is usually what is wanted
                    group = "${{ github.workflow }} @ ${{ github.event.pull_request.head.label || github.head_ref || github.run_id }}";
                }

                writer.WriteLine($"group: {group}");
                if (ConcurrencyCancelInProgress)
                {
                    writer.WriteLine("cancel-in-progress: true");
                }
            }
        }

        if (!DefaultShell.IsNullOrWhiteSpace())
        {
            writer.WriteLine();
            // defaults.run currently carries only shell; further run defaults (e.g. working-directory) slot in here
            writer.WriteLine("defaults:");
            using (writer.Indent())
            {
                writer.WriteLine("run:");
                using (writer.Indent())
                {
                    writer.WriteLine($"shell: {DefaultShell}");
                }
            }
        }

        writer.WriteLine();

        writer.WriteLine("jobs:");
        using (writer.Indent())
        {
            Jobs.ForEach(x => x.Write(writer));
        }
    }
}
