using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.Utilities;

namespace Fallout.Build.Execution.Extensions;

/// <summary>
/// Emits <c>build-graph.json</c> into the temporary directory on every build initialization,
/// giving editor tooling (the VS Code extension) a machine-readable projection of the target
/// graph: names, descriptions, the default/listed flags, the declaring type, and the four
/// relation kinds. Best-effort — a serialization failure never fails the build.
/// </summary>
internal class SerializeBuildGraphAttribute : BuildExtensionAttributeBase, IOnBuildInitialized
{
    private const string GraphFileName = "build-graph.json";

    /// <summary>Schema version consumers gate on; bump only on a breaking shape change.</summary>
    private const int SchemaVersion = 1;

    private static readonly JsonSerializerOptions serializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

    private AbsolutePath GraphFile => Build.TemporaryDirectory / GraphFileName;

    public void OnBuildInitialized(
        IReadOnlyCollection<ExecutableTarget> executableTargets,
        IReadOnlyCollection<ExecutableTarget> executionPlan)
    {
        try
        {
            var model = new BuildGraphModel(
                SchemaVersion,
                ResolveFalloutVersion(),
                executableTargets
                    .OrderBy(x => x.Name, StringComparer.Ordinal)
                    .Select(ToModel)
                    .ToList());

            GraphFile.WriteJson(model, s_options);
        }
        catch (Exception exception)
        {
            // Emission is a convenience for editor tooling — never let it break a build.
            Serilog.Log.Verbose(exception, "Failed to emit {GraphFileName}", GraphFileName);
        }
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

    // Mirrors Fallout.Migrate: the informational version up to the build-metadata separator,
    // so the pin aligns with the running tool. Null for local/dev builds without a `+` suffix.
    private static string ResolveFalloutVersion()
    {
        var informational = typeof(SerializeBuildGraphAttribute).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (string.IsNullOrEmpty(informational))
            return null;

        var plusIndex = informational.IndexOf('+');
        return plusIndex == -1 ? informational : informational[..plusIndex];
    }

    private sealed record BuildGraphModel(
        int Version,
        string FalloutVersion,
        IReadOnlyList<TargetModel> Targets);

    private sealed record TargetModel(
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
