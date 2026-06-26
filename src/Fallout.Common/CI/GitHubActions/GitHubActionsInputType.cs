using Fallout.Common.Tooling;

namespace Fallout.Common.CI.GitHubActions;

/// <summary>
/// The <c>type</c> of a <c>workflow_dispatch</c> input. See
/// <a href="https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions#onworkflow_dispatchinputs">workflow_dispatch.inputs</a>.
/// </summary>
public enum GitHubActionsInputType
{
    [EnumValue("string")] String,
    [EnumValue("boolean")] Boolean,
    [EnumValue("number")] Number,
    [EnumValue("choice")] Choice,
    [EnumValue("environment")] Environment
}
