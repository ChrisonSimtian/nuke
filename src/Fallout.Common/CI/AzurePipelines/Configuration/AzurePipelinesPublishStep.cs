using Fallout.Common.Utilities;

namespace Fallout.Common.CI.AzurePipelines.Configuration;

public class AzurePipelinesPublishStep : AzurePipelinesStep
{
    public string ArtifactName { get; set; }

    public string PathToPublish { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        using (writer.WriteBlock("- task: PublishBuildArtifacts@1"))
        {
            writer.WriteLine("displayName: " + $"Publish: {ArtifactName}".SingleQuoteYaml());
            using (writer.WriteBlock("inputs:"))
            {
                writer.WriteLine($"artifactName: {ArtifactName}");
                writer.WriteLine($"pathToPublish: {PathToPublish.SingleQuoteYaml()}");
            }
        }
    }
}
