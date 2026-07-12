using System.Collections.Generic;
using System.Linq;
using Fallout.Common.Utilities;

namespace Fallout.Common.CI.GitHubActions.Configuration;

/// <summary>
/// The per-job insertion surface handed to <see cref="IConfigureGitHubActions.ConfigureSteps"/>. Carries
/// the job's identity (<see cref="WorkflowName"/>, <see cref="Image"/>) and a read-only view of the job's
/// built-in steps, and collects the caller's insertions. Generator-constructed; not user-instantiable.
/// </summary>
public class GitHubActionsStepPipeline
{
    private readonly Dictionary<GitHubActionsStepPosition, List<GitHubActionsCustomStep>> _inserts =
        new Dictionary<GitHubActionsStepPosition, List<GitHubActionsCustomStep>>();

    internal GitHubActionsStepPipeline(string workflowName, GitHubActionsImage image, IReadOnlyList<GitHubActionsStep> builtInSteps)
    {
        WorkflowName = workflowName;
        Image = image;
        BuiltInSteps = builtInSteps;
    }

    /// <summary>The name of the workflow this job belongs to (normalized, spaces-to-underscores).</summary>
    public string WorkflowName { get; }

    /// <summary>The runner image of this job.</summary>
    public GitHubActionsImage Image { get; }

    /// <summary>A read-only view of the built-in steps already assembled for this job.</summary>
    public IReadOnlyList<GitHubActionsStep> BuiltInSteps { get; }

    /// <summary>Insert one custom step at <paramref name="position"/>. Multiple inserts at one position render in call order.</summary>
    public void Insert(GitHubActionsStepPosition position, GitHubActionsCustomStep step)
    {
        Assert.NotNull(step);
        if (!_inserts.TryGetValue(position, out var list))
            _inserts[position] = list = new List<GitHubActionsCustomStep>();
        list.Add(step);
    }

    /// <summary>Insert several custom steps at <paramref name="position"/>, in enumeration order.</summary>
    public void Insert(GitHubActionsStepPosition position, IEnumerable<GitHubActionsCustomStep> steps)
    {
        Assert.NotNull(steps);
        foreach (var step in steps)
            Insert(position, step);
    }

    internal IReadOnlyList<GitHubActionsCustomStep> GetInserts(GitHubActionsStepPosition position)
        => _inserts.TryGetValue(position, out var list) ? list : (IReadOnlyList<GitHubActionsCustomStep>)new GitHubActionsCustomStep[0];

    internal IEnumerable<GitHubActionsCustomStep> AllInserts => _inserts.Values.SelectMany(x => x);
}
