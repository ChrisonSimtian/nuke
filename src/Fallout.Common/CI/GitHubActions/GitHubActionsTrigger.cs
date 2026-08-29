using Fallout.Common.Tooling;

namespace Fallout.Common.CI.GitHubActions;

public enum GitHubActionsTrigger
{
    [EnumValue("push")]
    Push,

    [EnumValue("pull_request")]
    PullRequest,

    [EnumValue("workflow_dispatch")]
    WorkflowDispatch
}
