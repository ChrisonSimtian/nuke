using System.Linq;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI.AzurePipelines.Configuration;

public class AzurePipelinesStage : ConfigurationEntity
{
    public string Name { get; set; }

    public string DisplayName { get; set; }

    public AzurePipelinesImage? Image { get; set; }

    public AzurePipelinesStage[] Dependencies { get; set; }

    public AzurePipelinesJob[] Jobs { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        using (writer.WriteBlock($"- stage: {Name}"))
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

            using (writer.WriteBlock("jobs:"))
            {
                Jobs.ForEach(x => x.Write(writer));
            }
        }
    }
}
