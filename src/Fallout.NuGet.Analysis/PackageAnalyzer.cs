using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Versioning;

namespace Fallout.NuGet.Analysis;

/// <summary>
/// The analysis engine. Works purely off the resolved dependency graph(s) produced by
/// <see cref="ProjectAssetsReader"/>, so it sees exactly what NuGet resolved.
///
/// <para>The core rule unifies snitch's two notions of "redundant": a direct package reference
/// <c>X</c> is redundant when <c>X</c> is reachable in the resolved graph through some
/// <em>other</em> direct dependency — whether that dependency is a referenced project
/// (<see cref="FindingKind.RedundantViaProject"/>) or another package
/// (<see cref="FindingKind.RedundantViaPackage"/>).</para>
/// </summary>
public sealed class PackageAnalyzer
{
    /// <summary>Run redundancy detection on a single resolved project, plus cross-project version conflicts.</summary>
    public IReadOnlyList<Finding> Analyze(IEnumerable<AnalyzedProject> projects, AnalyzerOptions options = null)
    {
        options ??= new AnalyzerOptions();
        var materialized = projects
            .Where(x => options.TargetFramework == null ||
                        string.Equals(x.TargetFramework, options.TargetFramework, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var findings = new List<Finding>();
        foreach (var project in materialized)
            findings.AddRange(AnalyzeRedundancy(project, options));

        findings.AddRange(FindVersionConflicts(materialized, options));
        return findings;
    }

    /// <summary>Redundant direct package references for one project at one target framework.</summary>
    public IReadOnlyList<Finding> AnalyzeRedundancy(AnalyzedProject project, AnalyzerOptions options = null)
    {
        options ??= new AnalyzerOptions();
        var findings = new List<Finding>();

        var byName = BuildNameIndex(project.Graph);

        // The set of direct dependencies a redundant reference could be "provided" through.
        var directPackageKeys = project.DirectPackages
            .Where(x => x.NodeKey != null)
            .Select(x => x.NodeKey)
            .ToList();
        var allDirectKeys = directPackageKeys.Concat(project.DirectProjectNodeKeys).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var direct in project.DirectPackages)
        {
            if (direct.NodeKey == null)
                continue;
            if (direct.AutoReferenced || direct.PrivateAssetsAll)
                continue;
            if (options.ExcludedPackageIds.Contains(direct.Name))
                continue;

            // Every other direct dependency is a candidate provider.
            var otherRoots = allDirectKeys.Where(x => !string.Equals(x, direct.NodeKey, StringComparison.OrdinalIgnoreCase));

            var providerProjects = new List<string>();
            var providerPackages = new List<string>();
            foreach (var root in otherRoots)
            {
                var reachable = Reachable(project.Graph, byName, root);
                if (!reachable.Contains(direct.NodeKey))
                    continue;

                if (project.Graph.TryGetValue(root, out var rootNode))
                {
                    if (rootNode.IsProject)
                        providerProjects.Add(rootNode.Name);
                    else
                        providerPackages.Add(rootNode.Name);
                }
            }

            if (providerProjects.Count == 0 && providerPackages.Count == 0)
                continue;

            var resolvedVersion = project.Graph[direct.NodeKey].Version;
            var safe = IsSafeToRemove(project.Graph, direct, out var wouldResolveTo);

            var providers = providerProjects.Concat(providerPackages).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var kind = providerProjects.Count > 0 ? FindingKind.RedundantViaProject : FindingKind.RedundantViaPackage;
            var via = kind == FindingKind.RedundantViaProject ? "project reference" : "package";

            var detail = $"{direct.Name} is already provided via {via} {string.Join(", ", providers)}.";
            if (!safe)
                detail += $" Removing it may downgrade {direct.Name} from {resolvedVersion} to {wouldResolveTo}.";

            findings.Add(new Finding
            {
                Kind = kind,
                Project = project.ProjectName,
                TargetFramework = project.TargetFramework,
                PackageId = direct.Name,
                ResolvedVersion = resolvedVersion,
                DeclaredVersion = NormalizeRange(direct.VersionRange),
                SafeToRemove = safe,
                Providers = providers,
                Detail = detail,
            });
        }

        return findings;
    }

    /// <summary>
    /// Same package resolved at different versions across the analyzed projects.
    /// Detection is done <em>per target framework</em>, so a multi-targeted project that legitimately
    /// pins different versions across its own frameworks is not reported as a conflict.
    /// </summary>
    public IReadOnlyList<Finding> FindVersionConflicts(IReadOnlyList<AnalyzedProject> projects, AnalyzerOptions options = null)
    {
        options ??= new AnalyzerOptions();

        // package -> version -> projects, accumulated only from frameworks where the package actually conflicts.
        var conflicting = new Dictionary<string, Dictionary<string, SortedSet<string>>>(StringComparer.OrdinalIgnoreCase);

        foreach (var tfmGroup in projects.GroupBy(x => x.TargetFramework, StringComparer.OrdinalIgnoreCase))
        {
            var perPackage = new Dictionary<string, Dictionary<string, SortedSet<string>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var project in tfmGroup)
            {
                foreach (var node in project.Graph.Values)
                {
                    if (node.IsProject || string.IsNullOrEmpty(node.Version))
                        continue;
                    if (options.ExcludedPackageIds.Contains(node.Name))
                        continue;

                    if (!perPackage.TryGetValue(node.Name, out var byVersion))
                        perPackage[node.Name] = byVersion = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);
                    if (!byVersion.TryGetValue(node.Version, out var projectsOnVersion))
                        byVersion[node.Version] = projectsOnVersion = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                    projectsOnVersion.Add(project.ProjectName);
                }
            }

            foreach (var pair in perPackage)
            {
                if (pair.Value.Count < 2)
                    continue; // one resolved version within this framework — not a conflict

                if (!conflicting.TryGetValue(pair.Key, out var merged))
                    conflicting[pair.Key] = merged = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);

                foreach (var version in pair.Value)
                {
                    if (!merged.TryGetValue(version.Key, out var projectsOnVersion))
                        merged[version.Key] = projectsOnVersion = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var project in version.Value)
                        projectsOnVersion.Add(project);
                }
            }
        }

        var findings = new List<Finding>();
        foreach (var pair in conflicting)
        {
            var versions = pair.Value
                .OrderByDescending(x => ParseVersionOrZero(x.Key))
                .Select(x => new ConflictVersion(x.Key, x.Value.ToList()))
                .ToList();

            findings.Add(new Finding
            {
                Kind = FindingKind.VersionConflict,
                PackageId = pair.Key,
                ResolvedVersion = versions.First().Version,
                Providers = versions.Select(x => x.Version).ToList(),
                ConflictVersions = versions,
                Detail = $"{pair.Key} resolves to multiple versions: " +
                         string.Join("; ", versions.Select(v => $"{v.Version} ({string.Join(", ", v.Projects)})")) + ".",
            });
        }

        return findings;
    }

    private static NuGetVersion ParseVersionOrZero(string version)
    {
        return NuGetVersion.TryParse(version, out var parsed) ? parsed : new NuGetVersion(0, 0, 0);
    }

    private static bool IsSafeToRemove(
        IReadOnlyDictionary<string, GraphNode> graph,
        DirectDependency direct,
        out string wouldResolveTo)
    {
        wouldResolveTo = null;

        var declaredMin = ParseMinVersion(direct.VersionRange);
        if (declaredMin == null)
            return true;

        // Highest version any transitive requester asks for (edges in the graph pointing at this package).
        NuGetVersion highestTransitive = null;
        foreach (var node in graph.Values)
        {
            foreach (var edge in node.Dependencies)
            {
                if (!string.Equals(edge.Name, direct.Name, StringComparison.OrdinalIgnoreCase))
                    continue;

                var min = ParseMinVersion(edge.VersionRange);
                if (min != null && (highestTransitive == null || min > highestTransitive))
                    highestTransitive = min;
            }
        }

        if (highestTransitive == null)
            return true;

        if (declaredMin > highestTransitive)
        {
            wouldResolveTo = highestTransitive.ToNormalizedString();
            return false;
        }

        return true;
    }

    private static HashSet<string> Reachable(
        IReadOnlyDictionary<string, GraphNode> graph,
        IReadOnlyDictionary<string, string> byName,
        string startKey)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        queue.Enqueue(startKey);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!graph.TryGetValue(current, out var node))
                continue;

            foreach (var edge in node.Dependencies)
            {
                if (!byName.TryGetValue(edge.Name, out var childKey))
                    continue;
                if (visited.Add(childKey))
                    queue.Enqueue(childKey);
            }
        }

        return visited;
    }

    private static Dictionary<string, string> BuildNameIndex(IReadOnlyDictionary<string, GraphNode> graph)
    {
        var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in graph.Values)
            index[node.Name] = node.Key;
        return index;
    }

    private static NuGetVersion ParseMinVersion(string range)
    {
        if (string.IsNullOrWhiteSpace(range))
            return null;

        if (VersionRange.TryParse(range, out var parsed) && parsed.MinVersion != null)
            return parsed.MinVersion;

        return NuGetVersion.TryParse(range, out var version) ? version : null;
    }

    private static string NormalizeRange(string range)
    {
        var min = ParseMinVersion(range);
        return min != null ? min.ToNormalizedString() : range;
    }
}
