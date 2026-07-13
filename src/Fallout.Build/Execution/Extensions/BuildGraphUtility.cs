using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fallout.Common.Execution;
using Fallout.Common.Utilities;

namespace Fallout.Build.Execution.Extensions;

/// <summary>
/// Pure projection of the target graph into the <c>build-graph.json</c> shape consumed by editor
/// tooling (the VS Code extension): schema version, the running Fallout version, and for each target
/// its name, description, declaring type, the default/listed flags, and the four relation kinds.
/// <para>
/// This is the machine-readable contract the extension gates on — the JSON shape must stay stable.
/// Any breaking change to it requires bumping <see cref="SchemaVersion"/>. The projection is kept
/// separate from <see cref="SerializeBuildGraphAttribute"/> (which owns the build-lifecycle hook and
/// file I/O) so the contract can be snapshot-tested without driving a build.
/// </para>
/// </summary>
internal static class BuildGraphUtility
{
    /// <summary>Schema version consumers gate on; bump only on a breaking shape change.</summary>
    internal const int SchemaVersion = 1;

    private static readonly JsonSerializerOptions serializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

    /// <summary>Projects the targets into the serializable graph model.</summary>
    /// <param name="targets">The build's executable targets, in any order.</param>
    /// <param name="falloutVersion">The running Fallout version, or <c>null</c> for a local/dev build.</param>
    internal static BuildGraphModel GetModel(
        IReadOnlyCollection<ExecutableTarget> targets,
        string falloutVersion)
        => new(
            SchemaVersion,
            falloutVersion,
            targets
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .Select(ToModel)
                .ToList());

    /// <summary>Serializes the graph model to the exact JSON written into <c>build-graph.json</c>.</summary>
    internal static string GetJsonString(
        IReadOnlyCollection<ExecutableTarget> targets,
        string falloutVersion)
        => GetModel(targets, falloutVersion).ToJson(serializerOptions);

    // Takes the informational version up to the build-metadata separator ('+'), so the pin aligns with
    // the running tool. Returns the input unchanged when there is no separator, and null only when the
    // input is null/empty (e.g. a local build with no version stamped).
    internal static string NormalizeVersion(string informationalVersion)
    {
        if (string.IsNullOrEmpty(informationalVersion))
        {
            return null;
        }

        var plusIndex = informationalVersion.IndexOf('+');
        return plusIndex == -1 ? informationalVersion : informationalVersion[..plusIndex];
    }

    private static TargetModel ToModel(ExecutableTarget target)
        => new(
            target.Name,
            target.Description,
            target.Member?.DeclaringType?.Name,
            target.IsDefault,
            target.Listed,
            SortedNames(target.ExecutionDependencies),
            SortedNames(target.OrderDependencies),
            SortedNames(target.TriggerDependencies),
            SortedNames(target.Triggers));

    // Sorted for deterministic output — the graph carries no execution order, so the display
    // order is irrelevant to consumers and a stable ordering avoids spurious file churn.
    private static IReadOnlyList<string> SortedNames(IEnumerable<ExecutableTarget> targets)
        => targets.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal).ToList();

    internal sealed record BuildGraphModel(
        int Version,
        string FalloutVersion,
        IReadOnlyList<TargetModel> Targets);

    internal sealed record TargetModel(
        string Name,
        string Description,
        string DeclaredIn,
        bool Default,
        bool Listed,
        IReadOnlyList<string> DependsOn,
        IReadOnlyList<string> After,
        IReadOnlyList<string> TriggeredBy,
        IReadOnlyList<string> Triggers);
}
