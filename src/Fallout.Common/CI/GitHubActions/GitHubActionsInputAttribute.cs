using System;

namespace Fallout.Common.CI.GitHubActions;

/// <summary>
/// Declares a typed <c>workflow_dispatch</c> input. Repeatable on the build class and collected by the
/// <see cref="GitHubActionsAttribute"/> generator — the typed, compile-checked replacement for the
/// <c>OnWorkflowDispatch*Inputs</c> string arrays. Emits <c>type:</c>/<c>default:</c>/<c>options:</c>
/// into each matching workflow's <c>workflow_dispatch:</c> trigger.
/// <para/>
/// Scope with <see cref="Workflows"/>: empty applies the input to every dispatch workflow on the class;
/// otherwise only to the named ones. Misconfiguration (e.g. <see cref="GitHubActionsInputType.Choice"/>
/// without <see cref="Options"/>, a default outside the options, an unknown workflow name, a blank name)
/// fails generation loudly rather than emitting broken YAML.
/// <para/>
/// <see cref="Default"/> is emitted verbatim — for free-form <see cref="GitHubActionsInputType.String"/>
/// and <see cref="GitHubActionsInputType.Environment"/> inputs the caller owns YAML-correct quoting.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GitHubActionsInputAttribute : Attribute
{
    public GitHubActionsInputAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public GitHubActionsInputType Type { get; set; } = GitHubActionsInputType.String;
    public bool Required { get; set; }
    public string Default { get; set; }
    public string[] Options { get; set; } = new string[0];
    public string Description { get; set; }
    public string[] Workflows { get; set; } = new string[0];
}
