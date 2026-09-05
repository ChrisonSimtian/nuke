using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NuGet.Frameworks;

namespace Fallout.NuGet.Analysis;

/// <summary>
/// Reads a <c>project.assets.json</c> (the post-restore lock file) into one <see cref="AnalyzedProject"/>
/// per target framework. The assets file already encodes everything the analyzer needs: the declared
/// direct references (with <c>autoReferenced</c> / <c>suppressParent</c>), and the fully-resolved
/// transitive graph with the versions NuGet settled on.
/// </summary>
public static class ProjectAssetsReader
{
    /// <summary>
    /// Locate the assets file for a project (the default <c>&lt;projDir&gt;/obj/project.assets.json</c>).
    /// Returns <c>null</c> if it does not exist (i.e. the project has not been restored).
    /// </summary>
    public static string FindAssetsFile(string projectFilePath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(projectFilePath));
        if (dir == null)
            return null;

        var assets = Path.Combine(dir, "obj", "project.assets.json");
        return File.Exists(assets) ? assets : null;
    }

    /// <summary>Read every target framework out of the given assets file.</summary>
    public static IReadOnlyList<AnalyzedProject> Read(string assetsFilePath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(assetsFilePath));
        var root = document.RootElement;

        var projectElement = root.TryGetProperty("project", out var p) ? p : default;
        var restore = projectElement.ValueKind == JsonValueKind.Object && projectElement.TryGetProperty("restore", out var r)
            ? r
            : default;

        var projectPath = restore.ValueKind == JsonValueKind.Object && restore.TryGetProperty("projectPath", out var pp)
            ? pp.GetString()
            : assetsFilePath;
        var projectName = restore.ValueKind == JsonValueKind.Object && restore.TryGetProperty("projectName", out var pn)
            ? pn.GetString()
            : Path.GetFileNameWithoutExtension(projectPath);

        // Build a graph per resolved target (skipping RID-specific "tfm/rid" targets).
        var graphsByFramework = ReadGraphs(root);

        var results = new List<AnalyzedProject>();
        if (projectElement.ValueKind != JsonValueKind.Object ||
            !projectElement.TryGetProperty("frameworks", out var frameworks) ||
            frameworks.ValueKind != JsonValueKind.Object)
        {
            return results;
        }

        foreach (var framework in frameworks.EnumerateObject())
        {
            var shortTfm = ResolveShortTfm(framework);
            var nugetFramework = TryParseFramework(framework.Name);
            var graph = MatchGraph(graphsByFramework, nugetFramework);
            if (graph == null)
                continue;

            var directPackages = ReadDirectPackages(framework.Value, graph);
            var directProjectNodeKeys = ReadDirectProjectNodeKeys(restore, framework.Name, graph);

            results.Add(new AnalyzedProject(
                projectName,
                projectPath,
                shortTfm,
                directPackages,
                directProjectNodeKeys,
                graph));
        }

        return results;
    }

    private static Dictionary<NuGetFramework, IReadOnlyDictionary<string, GraphNode>> ReadGraphs(JsonElement root)
    {
        var result = new Dictionary<NuGetFramework, IReadOnlyDictionary<string, GraphNode>>();
        if (!root.TryGetProperty("targets", out var targets) || targets.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var target in targets.EnumerateObject())
        {
            // Skip RID-qualified targets such as ".NETCoreApp,Version=v10.0/win-x64".
            if (target.Name.Contains('/'))
                continue;

            var framework = TryParseFramework(target.Name);
            if (framework == null)
                continue;

            var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in target.Value.EnumerateObject())
            {
                var slash = node.Name.IndexOf('/');
                var name = slash >= 0 ? node.Name.Substring(0, slash) : node.Name;
                var version = slash >= 0 ? node.Name.Substring(slash + 1) : string.Empty;
                var type = node.Value.TryGetProperty("type", out var t) ? t.GetString() : "package";

                var edges = new List<Edge>();
                if (node.Value.TryGetProperty("dependencies", out var deps) && deps.ValueKind == JsonValueKind.Object)
                {
                    foreach (var dep in deps.EnumerateObject())
                        edges.Add(new Edge(dep.Name, dep.Value.GetString()));
                }

                nodes[node.Name] = new GraphNode(node.Name, name, version, type, edges);
            }

            result[framework] = nodes;
        }

        return result;
    }

    private static IReadOnlyDictionary<string, GraphNode> MatchGraph(
        Dictionary<NuGetFramework, IReadOnlyDictionary<string, GraphNode>> graphs,
        NuGetFramework framework)
    {
        if (framework != null && graphs.TryGetValue(framework, out var exact))
            return exact;

        // Single-target projects: pair the lone framework even if monikers parsed differently.
        return graphs.Count == 1 ? graphs.Values.First() : null;
    }

    private static IReadOnlyList<DirectDependency> ReadDirectPackages(
        JsonElement framework,
        IReadOnlyDictionary<string, GraphNode> graph)
    {
        var result = new List<DirectDependency>();
        if (!framework.TryGetProperty("dependencies", out var deps) || deps.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var dep in deps.EnumerateObject())
        {
            var value = dep.Value;
            var target = value.TryGetProperty("target", out var tg) ? tg.GetString() : "Package";
            if (!string.Equals(target, "Package", StringComparison.OrdinalIgnoreCase))
                continue;

            var versionRange = value.TryGetProperty("version", out var v) ? v.GetString() : null;
            var autoReferenced = value.TryGetProperty("autoReferenced", out var a) &&
                                 a.ValueKind == JsonValueKind.True;
            var suppressParent = value.TryGetProperty("suppressParent", out var sp) ? sp.GetString() : null;
            var privateAssetsAll = string.Equals(suppressParent, "All", StringComparison.OrdinalIgnoreCase);

            var nodeKey = FindNodeKeyByName(graph, dep.Name);
            result.Add(new DirectDependency(dep.Name, versionRange, autoReferenced, privateAssetsAll, nodeKey));
        }

        return result;
    }

    private static IReadOnlyList<string> ReadDirectProjectNodeKeys(
        JsonElement restore,
        string frameworkName,
        IReadOnlyDictionary<string, GraphNode> graph)
    {
        var result = new List<string>();
        if (restore.ValueKind != JsonValueKind.Object ||
            !restore.TryGetProperty("frameworks", out var frameworks) ||
            frameworks.ValueKind != JsonValueKind.Object ||
            !frameworks.TryGetProperty(frameworkName, out var framework) ||
            !framework.TryGetProperty("projectReferences", out var projectReferences) ||
            projectReferences.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var reference in projectReferences.EnumerateObject())
        {
            var projectFilePath = reference.Value.TryGetProperty("projectPath", out var pp)
                ? pp.GetString()
                : reference.Name;
            var name = Path.GetFileNameWithoutExtension(projectFilePath);

            var node = graph.Values.FirstOrDefault(x =>
                x.IsProject && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (node != null)
                result.Add(node.Key);
        }

        return result;
    }

    private static string FindNodeKeyByName(IReadOnlyDictionary<string, GraphNode> graph, string name)
    {
        return graph.Values
            .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
            ?.Key;
    }

    private static string ResolveShortTfm(JsonProperty framework)
    {
        if (framework.Value.TryGetProperty("targetAlias", out var alias))
        {
            var value = alias.GetString();
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return framework.Name;
    }

    private static NuGetFramework TryParseFramework(string moniker)
    {
        try
        {
            var framework = NuGetFramework.Parse(moniker);
            return framework.IsUnsupported ? null : framework;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
