using System.Collections.Generic;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI.AzurePipelines.Configuration;

public class AzurePipelinesCmdStep : AzurePipelinesStep
{
    public string[] InvokedTargets { get; set; }

    public string BuildCmdPath { get; set; }

    public int? PartitionSize { get; set; }

    public Dictionary<string, string> Imports { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        using (writer.WriteBlock("- task: CmdLine@2"))
        {
            writer.WriteLine("displayName: " + $"Run: {InvokedTargets.JoinCommaSpace()}".SingleQuoteYaml());

            var arguments = $"{InvokedTargets.JoinSpace()} --skip";
            if (PartitionSize != null)
            {
                arguments += $" --partition $(System.JobPositionInPhase)/{PartitionSize}";
            }

            using (writer.WriteBlock("inputs:"))
            {
                writer.WriteLine($"script: './{BuildCmdPath} {arguments}'");
            }

            if (Imports.Count > 0)
            {
                using (writer.WriteBlock("env:"))
                {
                    Imports.ForEach(x => writer.WriteLine($"{x.Key}: {x.Value}"));
                }
            }
        }
    }
}
