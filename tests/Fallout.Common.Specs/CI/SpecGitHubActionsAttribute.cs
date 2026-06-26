using System;
using System.Collections.Generic;
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

    public TestGitHubActionsAttribute(string name, GitHubActionsImage image, params GitHubActionsImage[] images)
        : base(name, image, images)
    {
    }

    public StreamWriter Stream { get; set; }

    // Injects typed inputs without static class-level attributes, so each test case can vary them.
    public GitHubActionsInputAttribute[] Inputs { get; set; } = new GitHubActionsInputAttribute[0];

    // The workflow names an input may scope to; defaults to just this workflow when unset.
    public string[] WorkflowNames { get; set; }

    protected override IEnumerable<GitHubActionsInputAttribute> DeclaredInputs => Inputs;

    protected override ISet<string> DeclaredWorkflowNames => (WorkflowNames ?? new[] { IdPostfix }).ToHashSet();

    protected override StreamWriter CreateStream()
    {
        return Stream;
    }
}
