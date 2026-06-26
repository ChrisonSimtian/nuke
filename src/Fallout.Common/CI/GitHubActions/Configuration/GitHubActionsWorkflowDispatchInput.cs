namespace Fallout.Common.CI.GitHubActions.Configuration;

public class GitHubActionsWorkflowDispatchInput
{
    public string Name { get; set; }
    public GitHubActionsInputType Type { get; set; }
    public bool Required { get; set; }
    public string Default { get; set; }
    public string[] Options { get; set; } = new string[0];
    public string Description { get; set; }
}
