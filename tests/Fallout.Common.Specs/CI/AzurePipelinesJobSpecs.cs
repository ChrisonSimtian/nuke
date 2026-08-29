using System.Threading.Tasks;
using Fallout.Common.CI.AzurePipelines;
using Fallout.Common.CI.AzurePipelines.Configuration;
using Xunit;

namespace Fallout.Common.Specs.CI;

public class AzurePipelinesJobSpecs
{
    [Fact]
    public Task DisplayName_with_an_apostrophe_is_yaml_escaped()
        => ConfigurationEntityVerifier.Verify(new AzurePipelinesJob
        {
            Name = "Compile",
            DisplayName = "Bob's Job",
            Dependencies = new AzurePipelinesJob[0],
            Steps = new AzurePipelinesStep[0]
        });

    [Fact]
    public Task VmImage_is_quoted_correctly_without_double_wrapping_quirk()
        => ConfigurationEntityVerifier.Verify(new AzurePipelinesJob
        {
            Name = "Compile",
            DisplayName = "Compile Job",
            Dependencies = new AzurePipelinesJob[0],
            Steps = new AzurePipelinesStep[0],
            Image = AzurePipelinesImage.Ubuntu2204
        });
}
