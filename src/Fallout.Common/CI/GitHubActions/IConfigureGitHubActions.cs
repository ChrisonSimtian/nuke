using Fallout.Common.CI.GitHubActions.Configuration;

namespace Fallout.Common.CI.GitHubActions;

/// <summary>
/// Implemented by a build to inject custom steps into generated GitHub Actions jobs. The generator calls
/// <see cref="ConfigureSteps"/> once per generated job, with a pipeline scoped to that job — so steps are
/// scoped to a workflow or runner by ordinary branching on <see cref="GitHubActionsStepPipeline.WorkflowName"/>
/// / <see cref="GitHubActionsStepPipeline.Image"/>, with no per-step scoping arrays. The generator stays in
/// sole control of the base step sequence; implementations only insert.
/// </summary>
public interface IConfigureGitHubActions
{
    void ConfigureSteps(GitHubActionsStepPipeline pipeline);
}
