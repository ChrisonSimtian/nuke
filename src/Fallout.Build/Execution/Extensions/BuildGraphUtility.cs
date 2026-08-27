using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Fallout.Common;
using Fallout.Common.Execution;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Fallout.Common.ValueInjection;

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
        => GetModel(targets, falloutVersion, new MemberInfo[0]);

    /// <summary>Projects the targets and the build's declared parameters into the serializable model.</summary>
    /// <param name="targets">The build's executable targets, in any order.</param>
    /// <param name="falloutVersion">The running Fallout version, or <c>null</c> for a local/dev build.</param>
    /// <param name="parameterMembers">
    /// The declared parameter members, from <see cref="ValueInjectionUtility.GetParameterMembers" /> —
    /// the same set <c>--help</c> lists, inherited component parameters included.
    /// </param>
    internal static BuildGraphModel GetModel(
        IReadOnlyCollection<ExecutableTarget> targets,
        string falloutVersion,
        IReadOnlyCollection<MemberInfo> parameterMembers)
        => new(
            SchemaVersion,
            falloutVersion,
            targets
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .Select(ToModel)
                .ToList(),
            parameterMembers
                .Select(ToModel)
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToList());

    /// <summary>Serializes the graph model to the exact JSON written into <c>build-graph.json</c>.</summary>
    internal static string GetJsonString(
        IReadOnlyCollection<ExecutableTarget> targets,
        string falloutVersion)
        => GetModel(targets, falloutVersion).ToJson(serializerOptions);

    /// <summary>Serializes the graph model, parameters included.</summary>
    internal static string GetJsonString(
        IReadOnlyCollection<ExecutableTarget> targets,
        string falloutVersion,
        IReadOnlyCollection<MemberInfo> parameterMembers)
        => GetModel(targets, falloutVersion, parameterMembers).ToJson(serializerOptions);

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
            SortedNames(target.Triggers),
            ToolRequirements(target));

    // Sorted for the same reason as SortedNames: the declaration order carries no meaning to a
    // consumer, and a stable ordering keeps build-graph.json free of spurious churn.
    private static IReadOnlyList<ToolRequirementModel> ToolRequirements(ExecutableTarget target)
        => target.ToolRequirements
            .Select(ToModel)
            .OrderBy(x => x.Kind, StringComparer.Ordinal)
            .ThenBy(x => x.PackageId, StringComparer.Ordinal)
            .ToList();

    // A path requirement names an executable rather than a package, and neither it nor an apt-get
    // requirement carries a version — both report null rather than inventing one.
    private static ToolRequirementModel ToModel(ToolRequirement requirement)
        => requirement switch
        {
            NuGetPackageRequirement x => new ToolRequirementModel("nuget", x.PackageId, x.Version),
            NpmPackageRequirement x => new ToolRequirementModel("npm", x.PackageId, x.Version),
            AptGetPackageRequirement x => new ToolRequirementModel("aptget", x.PackageId, Version: null),
            PathToolRequirement x => new ToolRequirementModel("path", x.PathExecutable, Version: null),
            _ => new ToolRequirementModel("unknown", requirement.GetType().Name, Version: null)
        };

    // Sorted for deterministic output — the graph carries no execution order, so the display
    // order is irrelevant to consumers and a stable ordering avoids spurious file churn.
    private static IReadOnlyList<string> SortedNames(IEnumerable<ExecutableTarget> targets)
        => targets.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal).ToList();

    // Projects one declared parameter. The value is deliberately absent: a [Secret] member's
    // injected value must never reach the emitted model, and emitting non-secret defaults only
    // would make the field's meaning depend on the flag next to it.
    private static ParameterModel ToModel(MemberInfo member)
    {
        var attribute = member.GetCustomAttribute<ParameterAttribute>();
        return new ParameterModel(
            ParameterService.GetParameterDashedName(member),
            UnderlyingTypeName(member.GetMemberType()),
            member.DeclaringType?.Name,
            ParameterService.GetParameterDescription(member),
            member.GetCustomAttribute<RequiredAttribute>() != null,
            member.GetCustomAttribute<SecretAttribute>() != null,
            attribute?.List ?? true,
            Default: null,
            AllowedValues: null);
    }

    // `int?` and `int` describe the same thing to someone typing --retries 3, so the wrapper is
    // unwrapped. Note this is NOT ReflectionUtility.GetNullableType, which goes the other way
    // (it *wraps* a value type) and throws outright on an interface-typed member.
    private static string UnderlyingTypeName(Type type)
        => (Nullable.GetUnderlyingType(type) ?? type).FullName;

    internal sealed record BuildGraphModel(
        int Version,
        string FalloutVersion,
        IReadOnlyList<TargetModel> Targets,
        IReadOnlyList<ParameterModel> Parameters);

    /// <summary>
    /// One declared <c>[Parameter]</c>. <paramref name="Name" /> is the dashed spelling a consumer
    /// types; <paramref name="Type" /> the CLR type with <see cref="Nullable{T}" /> unwrapped.
    /// <paramref name="Default" /> is always null — see <c>ToModel</c> for why.
    /// </summary>
    internal sealed record ParameterModel(
        string Name,
        string Type,
        string DeclaredIn,
        string Description,
        bool Required,
        bool Secret,
        bool List,
        string Default,
        IReadOnlyList<string> AllowedValues);

    internal sealed record TargetModel(
        string Name,
        string Description,
        string DeclaredIn,
        bool Default,
        bool Listed,
        IReadOnlyList<string> DependsOn,
        IReadOnlyList<string> After,
        IReadOnlyList<string> TriggeredBy,
        IReadOnlyList<string> Triggers,
        IReadOnlyList<ToolRequirementModel> ToolRequirements);

    /// <summary>
    /// One declared tool dependency. <paramref name="Kind" /> is <c>nuget</c>, <c>npm</c>,
    /// <c>aptget</c> or <c>path</c>; <paramref name="PackageId" /> carries the executable name for
    /// a path requirement. <paramref name="Version" /> is null for the kinds that have none.
    /// </summary>
    internal sealed record ToolRequirementModel(string Kind, string PackageId, string Version);
}
