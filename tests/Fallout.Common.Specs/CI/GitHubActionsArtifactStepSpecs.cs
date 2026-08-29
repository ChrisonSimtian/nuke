using System.Threading.Tasks;
using Fallout.Common.CI.GitHubActions.Configuration;
using Xunit;

namespace Fallout.Common.Specs.CI;

public class GitHubActionsArtifactStepSpecs
{
    [Fact]
    public Task Name_with_an_apostrophe_is_yaml_escaped()
        => ConfigurationEntityVerifier.Verify(new GitHubActionsArtifactStep
        {
            Name = "Bob's Build",
            Path = "out",
            Condition = ""
        });
}
