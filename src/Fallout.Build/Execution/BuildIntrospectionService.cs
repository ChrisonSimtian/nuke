using System;
using System.Collections.Generic;
using System.Reflection;
using Fallout.Build.Execution.Extensions;
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
    /// <summary>Whether this invocation is a read-only introspection request rather than a build.</summary>
    // --plan alone keeps its existing meaning (the HTML graph); only --json redirects it here.
    internal static bool IsRequested(FalloutBuild build)
        => build.Describe || (build.Plan && build.Json);

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

    // Mirrors SerializeBuildGraphAttribute: the informational version of the running Fallout
    // assembly, up to the build-metadata separator. Null when unstamped (a local/dev build).
    private static string FindFalloutVersion()
        => BuildGraphUtility.NormalizeVersion(
            typeof(BuildIntrospectionService).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion);
}
