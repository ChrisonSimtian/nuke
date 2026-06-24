using System.Collections.Generic;

namespace Fallout.NuGet.Analysis;

/// <summary>The category of a <see cref="Finding"/>.</summary>
public enum FindingKind
{
    /// <summary>A direct package reference already provided by a referenced project.</summary>
    RedundantViaProject,

    /// <summary>A direct package reference already pulled in transitively by another package reference.</summary>
    RedundantViaPackage,

    /// <summary>The same package resolves to different versions across the analyzed projects.</summary>
    VersionConflict,
}

/// <summary>Options controlling an analysis run.</summary>
public sealed class AnalyzerOptions
{
    /// <summary>Package ids to ignore entirely (case-insensitive).</summary>
    public ISet<string> ExcludedPackageIds { get; } =
        new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>When set, only this target framework (short form, e.g. <c>net10.0</c>) is analyzed.</summary>
    public string TargetFramework { get; set; }
}

/// <summary>A single resolved node in a project's dependency graph.</summary>
public sealed class GraphNode
{
    public GraphNode(string key, string name, string version, string type, IReadOnlyList<Edge> dependencies)
    {
        Key = key;
        Name = name;
        Version = version;
        Type = type;
        Dependencies = dependencies;
    }

    /// <summary>The assets-file node key, e.g. <c>Newtonsoft.Json/13.0.3</c>.</summary>
    public string Key { get; }

    public string Name { get; }

    /// <summary>The resolved version NuGet settled on for this node.</summary>
    public string Version { get; }

    /// <summary><c>package</c> or <c>project</c>.</summary>
    public string Type { get; }

    public IReadOnlyList<Edge> Dependencies { get; }

    public bool IsProject => string.Equals(Type, "project", System.StringComparison.OrdinalIgnoreCase);
}

/// <summary>An edge from one node to a dependency (by name, with the requested version range).</summary>
public sealed class Edge
{
    public Edge(string name, string versionRange)
    {
        Name = name;
        VersionRange = versionRange;
    }

    public string Name { get; }

    /// <summary>The range the parent requested, e.g. <c>[12.0.1, )</c>.</summary>
    public string VersionRange { get; }
}

/// <summary>A direct dependency declared by the project (a <c>PackageReference</c>).</summary>
public sealed class DirectDependency
{
    public DirectDependency(string name, string versionRange, bool autoReferenced, bool privateAssetsAll, string nodeKey)
    {
        Name = name;
        VersionRange = versionRange;
        AutoReferenced = autoReferenced;
        PrivateAssetsAll = privateAssetsAll;
        NodeKey = nodeKey;
    }

    public string Name { get; }

    /// <summary>The declared range, e.g. <c>[13.0.3, )</c>.</summary>
    public string VersionRange { get; }

    /// <summary>SDK-implicit reference — cannot be removed from the csproj.</summary>
    public bool AutoReferenced { get; }

    /// <summary><c>PrivateAssets="all"</c> — does not flow to consumers.</summary>
    public bool PrivateAssetsAll { get; }

    /// <summary>The resolved graph node this reference maps to (may be <c>null</c> if unresolved).</summary>
    public string NodeKey { get; }
}

/// <summary>The analyzable state of one project at one target framework, read from <c>project.assets.json</c>.</summary>
public sealed class AnalyzedProject
{
    public AnalyzedProject(
        string projectName,
        string projectPath,
        string targetFramework,
        IReadOnlyList<DirectDependency> directPackages,
        IReadOnlyList<string> directProjectNodeKeys,
        IReadOnlyDictionary<string, GraphNode> graph)
    {
        ProjectName = projectName;
        ProjectPath = projectPath;
        TargetFramework = targetFramework;
        DirectPackages = directPackages;
        DirectProjectNodeKeys = directProjectNodeKeys;
        Graph = graph;
    }

    public string ProjectName { get; }

    public string ProjectPath { get; }

    /// <summary>Short form, e.g. <c>net10.0</c>.</summary>
    public string TargetFramework { get; }

    public IReadOnlyList<DirectDependency> DirectPackages { get; }

    /// <summary>Graph node keys of the project's direct <c>ProjectReference</c>s.</summary>
    public IReadOnlyList<string> DirectProjectNodeKeys { get; }

    /// <summary>The resolved dependency graph keyed by node key (<c>Name/Version</c>).</summary>
    public IReadOnlyDictionary<string, GraphNode> Graph { get; }
}

/// <summary>A single analyzer finding.</summary>
public sealed class Finding
{
    public FindingKind Kind { get; set; }

    /// <summary>The project the finding applies to (the one carrying the redundant reference).</summary>
    public string Project { get; set; }

    public string TargetFramework { get; set; }

    public string PackageId { get; set; }

    /// <summary>The resolved version in the graph.</summary>
    public string ResolvedVersion { get; set; }

    /// <summary>The version declared on the direct reference (redundancy findings only).</summary>
    public string DeclaredVersion { get; set; }

    /// <summary>
    /// <c>true</c> when the reference can be removed without changing the resolved version;
    /// <c>false</c> when removal might lower the resolved version (advisory). Redundancy findings only.
    /// </summary>
    public bool SafeToRemove { get; set; }

    /// <summary>The projects/packages that already provide this package.</summary>
    public IReadOnlyList<string> Providers { get; set; } = new List<string>();

    /// <summary>A human-readable explanation.</summary>
    public string Detail { get; set; }
}
