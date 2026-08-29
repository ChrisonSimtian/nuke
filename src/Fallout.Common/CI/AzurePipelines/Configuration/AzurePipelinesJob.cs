using System.Linq;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI.AzurePipelines.Configuration;

public class AzurePipelinesJob : ConfigurationEntity
{
    public string Name { get; set; }

    public string DisplayName { get; set; }

    public AzurePipelinesImage? Image { get; set; }

    public AzurePipelinesJob[] Dependencies { get; set; }

    public int Parallel { get; set; }

    public AzurePipelinesStep[] Steps { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        using (writer.WriteBlock($"- job: {Name}"))
        {
            writer.WriteLine($"displayName: {DisplayName.SingleQuoteYaml()}");
            writer.WriteLine($"dependsOn: [ {Dependencies.Select(x => x.Name).JoinCommaSpace()} ]");

            if (Image != null)
            {
                using (writer.WriteBlock("pool:"))
                {
                    writer.WriteLine($"vmImage: {Image.Value.GetValue().SingleQuoteYaml()}");
                }
            }

            if (Parallel > 1)
            {
                using (writer.WriteBlock("strategy:"))
                {
                    writer.WriteLine($"parallel: {Parallel}");
                }
            }

            using (writer.WriteBlock("steps:"))
            {
                Steps.ForEach(x => x.Write(writer));
            }
        }
    }
}
