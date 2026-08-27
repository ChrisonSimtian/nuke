using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Fallout.Build.Execution.Extensions;
using Fallout.Common.Utilities;
using Fallout.Common.ValueInjection;

namespace Fallout.Common.Execution;

/// <summary>
/// Owns the read-only introspection requests — <c>--describe</c> and <c>--plan --json</c> — which
/// print the build model on standard output and execute nothing.
/// <para>
/// <see cref="BuildManager" /> calls this immediately after the execution plan is resolved and
/// <em>before</em> <see cref="ToolRequirementService.EnsureToolRequirements" />. That ordering is the
/// feature, not an implementation detail: <c>EnsureToolRequirements</c> writes into the temporary
/// directory and shells out to <c>dotnet restore</c>, so an <see cref="IOnBuildInitialized" />
/// extension — where <c>--help</c> and <c>--plan</c> live — could not honour "runs no external tool".
/// </para>
/// </summary>
internal static class BuildIntrospectionService
{
    // The exact reason string BuildExecutor records, so the predicted plan and the executed one
    // describe a --skip the same way.
    private const string SkippedViaParameter = "via parameter";

    private static readonly JsonSerializerOptions serializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

    /// <summary>Whether this invocation is a read-only introspection request rather than a build.</summary>
    // --plan alone keeps its existing meaning (the HTML graph); only --json redirects it here.
    internal static bool IsRequested(FalloutBuild build)
        => build.Describe || (build.Plan && build.Json);

    /// <summary>The document for whichever request <see cref="IsRequested" /> matched.</summary>
    internal static string GetDocument(
        FalloutBuild build,
        IReadOnlyCollection<ExecutableTarget> targets,
        IReadOnlyCollection<ExecutableTarget> plan)
        => build.Describe
            ? GetDescribeJson(build, targets)
            : GetPlanJson(
                ParameterService.GetParameter<string[]>(() => build.InvokedTargets) ?? new string[0],
                plan,
                ParameterService.GetParameter<string[]>(() => build.SkippedTargets));

    /// <summary>
    /// The resolved execution plan: what <em>would</em> run, in order, and what gates each entry.
    /// Conditions are reported as their declared text and never evaluated — they are user delegates,
    /// and running them would contradict "invokes no target".
    /// </summary>
    internal static string GetPlanJson(
        IReadOnlyCollection<string> invokedTargets,
        IReadOnlyCollection<ExecutableTarget> plan,
        IReadOnlyCollection<string> skippedTargets)
    {
        // Mirrors BuildExecutor: dashes are stripped before matching, and an empty --skip list
        // means "skip everything".
        var skipped = (skippedTargets ?? new string[0])
            .Select(x => x.Replace("-", string.Empty)).ToList();

        var entries = plan
            .Select((target, index) => new PlanEntryModel(
                target.Name,
                index,
                target.Invoked,
                skippedTargets != null &&
                (skipped.Count == 0 || skipped.Contains(target.Name, StringComparer.OrdinalIgnoreCase))
                    ? SkippedViaParameter
                    : null,
                target.StaticConditions.Select(x => x.Text).ToList(),
                target.DynamicConditions.Select(x => x.Text).ToList()))
            .ToList();

        return new PlanModel(
                BuildGraphUtility.SchemaVersion,
                invokedTargets.ToList(),
                entries)
            .ToJson(serializerOptions);
    }

    /// <summary>The whole build model: targets, dependency edges, tool requirements, parameters.</summary>
    internal static string GetDescribeJson(
        FalloutBuild build,
        IReadOnlyCollection<ExecutableTarget> targets)
        => GetDescribeJson(build, targets, FindFalloutVersion());

    /// <summary>Overload taking an explicit version, so the document can be asserted deterministically.</summary>
    internal static string GetDescribeJson(
        FalloutBuild build,
        IReadOnlyCollection<ExecutableTarget> targets,
        string falloutVersion)
        => BuildGraphUtility.GetJsonString(
            targets,
            falloutVersion,
            ValueInjectionUtility.GetParameterMembers(build.GetType(), includeUnlisted: false));

    internal sealed record PlanModel(
        int Version,
        IReadOnlyList<string> InvokedTargets,
        IReadOnlyList<PlanEntryModel> Plan);

    /// <summary>
    /// One entry of the resolved plan. <paramref name="Order" /> is its position in the run,
    /// <paramref name="Invoked" /> distinguishes an explicitly requested target from one pulled in
    /// as a dependency, and <paramref name="Skip" /> is null unless <c>--skip</c> names it.
    /// </summary>
    internal sealed record PlanEntryModel(
        string Name,
        int Order,
        bool Invoked,
        string Skip,
        IReadOnlyList<string> StaticConditions,
        IReadOnlyList<string> DynamicConditions);

    // Mirrors SerializeBuildGraphAttribute: the informational version of the running Fallout
    // assembly, up to the build-metadata separator. Null when unstamped (a local/dev build).
    private static string FindFalloutVersion()
        => BuildGraphUtility.NormalizeVersion(
            typeof(BuildIntrospectionService).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion);
}
