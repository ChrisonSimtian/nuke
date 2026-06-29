using System;
using System.IO;
using System.Linq;
using Fallout.Common.CI.GitHubActions;

namespace Fallout.Common.Specs.CI;

public class TestGitHubActionsAttribute : GitHubActionsAttribute, ITestConfigurationGenerator
{
    public TestGitHubActionsAttribute(GitHubActionsImage image, params GitHubActionsImage[] images)
        : base("test", image, images)
    {
    }

    public StreamWriter Stream { get; set; }

    protected override StreamWriter CreateStream()
    {
        return Stream;
    }
}
