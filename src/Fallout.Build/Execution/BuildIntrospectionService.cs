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
    /// <summary>
    /// Version of the <c>--plan --json</c> document. Deliberately its own constant rather than
    /// <see cref="BuildGraphUtility.SchemaVersion" />: the plan and <c>build-graph.json</c> are
    /// different shapes, and one number could not tell a consumer which contract it received —
    /// nor could it be bumped for one without falsely signalling a break in the other.
    /// </summary>
    internal const int PlanSchemaVersion = 1;

    /// <summary>Version of the error envelope, separate for the same reason.</summary>
    internal const int ErrorSchemaVersion = 1;

    /// <summary>Whether this invocation is a read-only introspection request rather than a build.</summary>
    /// <remarks>
    /// Each flag is read from the injected property OR straight from the arguments, because this is
    /// asked <em>before</em> value injection has run: InjectParameterValuesAttribute is itself an
    /// IOnBuildCreated extension, and the gate has to fire before any extension does. Reading only
    /// the property here would make every request look like an ordinary build.
    /// </remarks>
    // --plan alone keeps its existing meaning (the HTML graph); only --json redirects it here.
    internal static bool IsRequested(FalloutBuild build)
        => Flag(build.Describe, nameof(FalloutBuild.Describe)) ||
           (Flag(build.Plan, nameof(FalloutBuild.Plan)) && Flag(build.Json, nameof(FalloutBuild.Json)));

    private static bool Flag(bool injected, string parameterName)
        => injected || ParameterService.GetParameter<bool>(parameterName);

    /// <summary>The document for whichever request <see cref="IsRequested" /> matched.</summary>
    internal static string GetDocument(
        FalloutBuild build,
        IReadOnlyCollection<ExecutableTarget> targets,
        IReadOnlyCollection<ExecutableTarget> plan)
        => Flag(build.Describe, nameof(FalloutBuild.Describe))
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
                // MarkTargetSkipped only skips when !Invoked, so an explicitly invoked target runs
                // even when --skip names it. The prediction has to agree with the executor.
                !target.Invoked &&
                skippedTargets != null &&
                (skipped.Count == 0 || skipped.Contains(target.Name, StringComparer.OrdinalIgnoreCase))
                    ? BuildExecutor.SkippedViaParameterReason
                    : null,
                target.StaticConditions.Select(x => x.Text).ToList(),
                target.DynamicConditions.Select(x => x.Text).ToList()))
            .ToList();

        return new PlanModel(
                PlanSchemaVersion,
                invokedTargets.ToList(),
                entries)
            .ToJson(BuildGraphUtility.SerializerOptions);
    }

    /// <summary>The whole build model: targets, dependency edges, tool requirements, parameters.</summary>
    internal static string GetDescribeJson(
        FalloutBuild build,
        IReadOnlyCollection<ExecutableTarget> targets)
        => BuildGraphUtility.GetJsonString(build, targets);

    /// <summary>Overload taking an explicit version, so the document can be asserted deterministically.</summary>
    internal static string GetDescribeJson(
        FalloutBuild build,
        IReadOnlyCollection<ExecutableTarget> targets,
        string falloutVersion)
        => BuildGraphUtility.GetModel(
                targets,
                falloutVersion,
                ValueInjectionUtility.GetParameterMembers(build.GetType(), includeUnlisted: false))
            .ToJson(BuildGraphUtility.SerializerOptions);

    /// <summary>
    /// The failure form of both documents, so a consumer parsing standard output gets JSON whether
    /// the request succeeded or the build threw on its way to being described.
    /// </summary>
    internal static string GetErrorJson(Exception exception)
        => new ErrorModel(
                ErrorSchemaVersion,
                new ErrorDetailModel(exception.GetType().Name, exception.Message))
            .ToJson(BuildGraphUtility.SerializerOptions);

    internal sealed record ErrorModel(int Version, ErrorDetailModel Error);

    internal sealed record ErrorDetailModel(string Kind, string Message);

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
}
