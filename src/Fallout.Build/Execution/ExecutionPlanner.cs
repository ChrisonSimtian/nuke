using System.Collections.Generic;
using System.Linq;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using Fallout.Core.Planning;

namespace Fallout.Common.Execution;

/// <summary>
/// Given the invoked target names, creates an execution plan under consideration of execution, ordering and trigger dependencies.
/// </summary>
internal static class ExecutionPlanner
{
    public static IReadOnlyCollection<ExecutableTarget> GetExecutionPlan(
        IReadOnlyCollection<ExecutableTarget> executableTargets,
        IReadOnlyCollection<string> invokedTargetNames)
    {
        var invokedTargets = invokedTargetNames?.Select(x => GetExecutableTarget(x, executableTargets)).ToList();
        invokedTargets?.ForEach(x => x.Invoked = true);

        // Repeat to create the plan with triggers taken into account until plan doesn't change
        IReadOnlyCollection<ExecutableTarget> executionPlan;
        IReadOnlyCollection<ExecutableTarget> additionallyTriggered;
        do
        {
            executionPlan = GetExecutionPlanInternal(executableTargets, invokedTargets);
            additionallyTriggered = executionPlan
                .SelectMany(x => x.Triggers)
                .Except(executionPlan)
                .Where(executableTargets.Contains).ToList();

            invokedTargets = executionPlan.Concat(additionallyTriggered).ToList();
        }
        while (additionallyTriggered.Count > 0);

        return executionPlan.ForEachLazy(x => x.Status = ExecutionStatus.Scheduled).ToList();
    }

    private static IReadOnlyCollection<ExecutableTarget> GetExecutionPlanInternal(
        IReadOnlyCollection<ExecutableTarget> executableTargets,
        ICollection<ExecutableTarget> invokedTargets)
    {
        // Pure graph work — cycle detection and topological ordering — lives in Fallout.Core.
        // Everything below is orchestration: deciding to fail the build, and applying the domain
        // scheduling rules (invoked / default / execution-dependency) over the ordered nodes.
        var strict = ParameterService.GetNamedArgument<bool>("strict");
        var plan = TopoSort.Order(executableTargets, x => x.AllDependencies, strict);

        if (plan.HasCycles)
        {
            // TODO: logging additional
            Assert.Fail("Circular dependencies between targets:"
                .Concat(plan.Cycles.Select(x => $" - {x.Select(y => y.Name).JoinCommaSpace()}"))
                .JoinNewLine());
        }

        if (plan.IsAmbiguous)
        {
            // TODO: logging additional
            Assert.Fail("Incomplete target definition order:"
                .Concat(plan.AmbiguousStep.Select(x => $"  - {x.Name}"))
                .JoinNewLine());
        }

        var scheduledTargets = new List<ExecutableTarget>();
        foreach (var executableTarget in plan.Ordered)
        {
            if (!(invokedTargets != null && invokedTargets.Contains(executableTarget)) &&
                !(invokedTargets == null && executableTarget.IsDefault) &&
                !scheduledTargets.SelectMany(x => x.ExecutionDependencies).Contains(executableTarget))
            {
                continue;
            }

            scheduledTargets.Add(executableTarget);
        }

        scheduledTargets.Reverse();

        return scheduledTargets;
    }

    private static ExecutableTarget GetExecutableTarget(
        string targetName,
        IReadOnlyCollection<ExecutableTarget> executableTargets)
    {
        targetName = targetName.Replace("-", string.Empty);
        var executableTarget = executableTargets.SingleOrDefault(x => x.Name.EqualsOrdinalIgnoreCase(targetName));
        if (executableTarget == null)
        {
            Assert.Fail($"Target with name {targetName.SingleQuote()} does not exist. Available targets are:"
                .Concat(executableTargets.Select(x => $"  - {x.Name}").OrderBy(x => x))
                .JoinNewLine());
        }

        return executableTarget;
    }
}
