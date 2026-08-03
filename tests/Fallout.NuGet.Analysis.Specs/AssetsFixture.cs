using System;
using System.IO;
using System.Linq;

namespace Fallout.NuGet.Analysis.Specs;

/// <summary>
/// Builds synthetic <c>project.assets.json</c> content for the analyzer specs and writes it to a
/// temp file. Fragments (<c>Target</c>, <c>DirectDependencies</c>, <c>ProjectReferences</c>) are raw
/// JSON snippets, so a test reads like the shape of a real assets file.
/// </summary>
internal static class AssetsFixture
{
    /// <summary>One target framework of a project: its resolved graph and declared direct references.</summary>
    internal sealed record Framework(
        string Tfm,
        string DirectDependencies,
        string Target,
        string ProjectReferences = null,
        string Alias = null);

    public static string WriteAssets(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"assets-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    /// <summary>A single-target-framework assets file (the common case).</summary>
    public static string SingleFramework(
        string directDependencies,
        string target,
        string projectReferences = null,
        string projectName = "TestProject",
        string tfm = "net10.0")
        => Assets(projectName, new Framework(tfm, directDependencies, target, projectReferences));

    /// <summary>A multi-target-framework assets file.</summary>
    public static string Assets(string projectName, params Framework[] frameworks)
        => Assets(projectName, extraTargets: null, frameworks);

    /// <summary>
    /// A multi-target-framework assets file, plus optional raw extra entries under <c>targets</c>
    /// (used to inject RID-qualified targets the reader is expected to skip).
    /// </summary>
    public static string Assets(string projectName, string extraTargets, params Framework[] frameworks)
    {
        static string Alias(Framework framework) => framework.Alias ?? framework.Tfm;

        var targets = string.Join(",\n", frameworks.Select(framework => $$"""
            "{{framework.Tfm}}": {
                {{framework.Target}}
            }
            """));
        if (!string.IsNullOrEmpty(extraTargets))
            targets += ",\n" + extraTargets;

        var restoreFrameworks = string.Join(",\n", frameworks.Select(framework =>
        {
            var projectReferences = framework.ProjectReferences == null
                ? string.Empty
                : $$""", "projectReferences": { {{framework.ProjectReferences}} }""";
            return $$"""
                "{{framework.Tfm}}": { "targetAlias": "{{Alias(framework)}}"{{projectReferences}} }
                """;
        }));

        var projectFrameworks = string.Join(",\n", frameworks.Select(framework => $$"""
            "{{framework.Tfm}}": {
                "targetAlias": "{{Alias(framework)}}",
                "dependencies": {
                    {{framework.DirectDependencies}}
                }
            }
            """));

        return $$"""
            {
                "version": 3,
                "targets": {
                    {{targets}}
                },
                "project": {
                    "version": "1.0.0",
                    "restore": {
                        "projectName": "{{projectName}}",
                        "projectPath": "/repo/{{projectName}}/{{projectName}}.csproj",
                        "frameworks": {
                            {{restoreFrameworks}}
                        }
                    },
                    "frameworks": {
                        {{projectFrameworks}}
                    }
                }
            }
            """;
    }
}
