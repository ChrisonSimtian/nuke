using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Fallout.NuGet.Analysis.Specs;

public sealed class PackageAnalyzerSpecs
{
    [Fact]
    public void Detects_transitive_package_redundancy_and_marks_it_safe()
    {
        // Direct refs A and B; A depends on B at the same version B resolves to.
        var assets = WriteAssets(Scenario(
            directDependencies: """
                                "A": { "target": "Package", "version": "[1.0.0, )" },
                                "B": { "target": "Package", "version": "[1.0.0, )" }
                                """,
            target: """
                    "A/1.0.0": { "type": "package", "dependencies": { "B": "1.0.0" } },
                    "B/1.0.0": { "type": "package" }
                    """));

        var findings = Analyze(assets);

        var finding = findings.Single(x => x.PackageId == "B");
        finding.Kind.Should().Be(FindingKind.RedundantViaPackage);
        finding.Providers.Should().ContainSingle().Which.Should().Be("A");
        finding.SafeToRemove.Should().BeTrue();
    }

    [Fact]
    public void Flags_might_downgrade_when_the_direct_reference_pins_higher_than_the_transitive_one()
    {
        // B is pinned directly at 2.0.0 (so it resolves to 2.0.0) but A only asks for 1.0.0.
        var assets = WriteAssets(Scenario(
            directDependencies: """
                                "A": { "target": "Package", "version": "[1.0.0, )" },
                                "B": { "target": "Package", "version": "[2.0.0, )" }
                                """,
            target: """
                    "A/1.0.0": { "type": "package", "dependencies": { "B": "1.0.0" } },
                    "B/2.0.0": { "type": "package" }
                    """));

        var finding = Analyze(assets).Single(x => x.PackageId == "B");

        finding.SafeToRemove.Should().BeFalse();
        finding.ResolvedVersion.Should().Be("2.0.0");
        finding.Detail.Should().Contain("downgrade");
    }

    [Fact]
    public void Ignores_auto_referenced_and_private_assets_dependencies()
    {
        var assets = WriteAssets(Scenario(
            directDependencies: """
                                "A": { "target": "Package", "version": "[1.0.0, )" },
                                "B": { "target": "Package", "version": "[1.0.0, )", "autoReferenced": true },
                                "C": { "target": "Package", "version": "[1.0.0, )", "suppressParent": "All" }
                                """,
            target: """
                    "A/1.0.0": { "type": "package", "dependencies": { "B": "1.0.0", "C": "1.0.0" } },
                    "B/1.0.0": { "type": "package" },
                    "C/1.0.0": { "type": "package" }
                    """));

        Analyze(assets).Should().BeEmpty();
    }

    [Fact]
    public void Respects_the_exclude_option()
    {
        var assets = WriteAssets(Scenario(
            directDependencies: """
                                "A": { "target": "Package", "version": "[1.0.0, )" },
                                "B": { "target": "Package", "version": "[1.0.0, )" }
                                """,
            target: """
                    "A/1.0.0": { "type": "package", "dependencies": { "B": "1.0.0" } },
                    "B/1.0.0": { "type": "package" }
                    """));

        var options = new AnalyzerOptions();
        options.ExcludedPackageIds.Add("B");

        Analyze(assets, options).Should().BeEmpty();
    }

    [Fact]
    public void Detects_redundancy_provided_through_a_project_reference()
    {
        var assets = WriteAssets(Scenario(
            directDependencies: """
                                "Newtonsoft.Json": { "target": "Package", "version": "[13.0.0, )" }
                                """,
            target: """
                    "MyLib/1.0.0": { "type": "project", "dependencies": { "Newtonsoft.Json": "13.0.0" } },
                    "Newtonsoft.Json/13.0.0": { "type": "package" }
                    """,
            projectReferences: """
                               "/repo/MyLib/MyLib.csproj": { "projectPath": "/repo/MyLib/MyLib.csproj" }
                               """));

        var finding = Analyze(assets).Single();

        finding.Kind.Should().Be(FindingKind.RedundantViaProject);
        finding.PackageId.Should().Be("Newtonsoft.Json");
        finding.Providers.Should().Contain("MyLib");
    }

    [Fact]
    public void Detects_version_conflicts_across_projects()
    {
        var projectOne = ProjectAssetsReader.Read(WriteAssets(Scenario(
            projectName: "ProjectOne",
            directDependencies: """ "Serilog": { "target": "Package", "version": "[3.0.0, )" } """,
            target: """ "Serilog/3.0.0": { "type": "package" } """)));

        var projectTwo = ProjectAssetsReader.Read(WriteAssets(Scenario(
            projectName: "ProjectTwo",
            directDependencies: """ "Serilog": { "target": "Package", "version": "[4.0.0, )" } """,
            target: """ "Serilog/4.0.0": { "type": "package" } """)));

        var conflicts = new PackageAnalyzer()
            .Analyze(projectOne.Concat(projectTwo).ToList())
            .Where(x => x.Kind == FindingKind.VersionConflict)
            .ToList();

        var serilog = conflicts.Single(x => x.PackageId == "Serilog");
        serilog.Providers.Should().BeEquivalentTo(new[] { "3.0.0", "4.0.0" });
    }

    private static IReadOnlyList<Finding> Analyze(string assetsFile, AnalyzerOptions options = null)
    {
        var projects = ProjectAssetsReader.Read(assetsFile);
        return new PackageAnalyzer().Analyze(projects, options)
            .Where(x => x.Kind != FindingKind.VersionConflict)
            .ToList();
    }

    private static string Scenario(
        string directDependencies,
        string target,
        string projectReferences = null,
        string projectName = "TestProject",
        string tfm = "net10.0")
    {
        var projectReferencesJson = projectReferences == null
            ? string.Empty
            : $$""""
                ,
                        "projectReferences": {
                            {{projectReferences}}
                        }
                """";

        return $$"""
                 {
                     "version": 3,
                     "targets": {
                         "net10.0": {
                             {{target}}
                         }
                     },
                     "project": {
                         "version": "1.0.0",
                         "restore": {
                             "projectName": "{{projectName}}",
                             "projectPath": "/repo/{{projectName}}/{{projectName}}.csproj",
                             "frameworks": {
                                 "{{tfm}}": {
                                     "targetAlias": "{{tfm}}"{{projectReferencesJson}}
                                 }
                             }
                         },
                         "frameworks": {
                             "{{tfm}}": {
                                 "targetAlias": "{{tfm}}",
                                 "dependencies": {
                                     {{directDependencies}}
                                 }
                             }
                         }
                     }
                 }
                 """;
    }

    private static string WriteAssets(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"assets-{System.Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
