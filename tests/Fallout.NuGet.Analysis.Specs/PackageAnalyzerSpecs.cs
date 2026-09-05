using System.Collections.Generic;
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

    [Fact]
    public void Detects_redundancy_reachable_through_a_multi_hop_transitive_chain()
    {
        // Direct refs A and B. A -> C -> B, so B is redundant via A across two hops.
        var assets = WriteAssets(Scenario(
            directDependencies: """
                                "A": { "target": "Package", "version": "[1.0.0, )" },
                                "B": { "target": "Package", "version": "[1.0.0, )" }
                                """,
            target: """
                    "A/1.0.0": { "type": "package", "dependencies": { "C": "1.0.0" } },
                    "C/1.0.0": { "type": "package", "dependencies": { "B": "1.0.0" } },
                    "B/1.0.0": { "type": "package" }
                    """));

        var finding = Analyze(assets).Single(x => x.PackageId == "B");

        finding.Kind.Should().Be(FindingKind.RedundantViaPackage);
        finding.Providers.Should().ContainSingle().Which.Should().Be("A");
        finding.SafeToRemove.Should().BeTrue();
    }

    [Fact]
    public void Does_not_flag_a_conflict_when_one_project_pins_different_versions_across_its_own_tfms()
    {
        // One project, two TFMs. Serilog resolves to 3.0.0 on net8.0 and 4.0.0 on net10.0.
        // That is legal multi-targeting, not a conflict — detection is per target framework.
        var assets = AssetsFixture.WriteAssets(AssetsFixture.Assets(
            "MultiTarget",
            new AssetsFixture.Framework(
                Tfm: "net8.0",
                DirectDependencies: """ "Serilog": { "target": "Package", "version": "[3.0.0, )" } """,
                Target: """ "Serilog/3.0.0": { "type": "package" } """),
            new AssetsFixture.Framework(
                Tfm: "net10.0",
                DirectDependencies: """ "Serilog": { "target": "Package", "version": "[4.0.0, )" } """,
                Target: """ "Serilog/4.0.0": { "type": "package" } """)));

        var projects = ProjectAssetsReader.Read(assets);
        projects.Should().HaveCount(2);

        var findings = new PackageAnalyzer().Analyze(projects);

        findings.Where(x => x.Kind == FindingKind.VersionConflict).Should().BeEmpty();
    }

    [Fact]
    public void Reports_a_conflict_only_from_the_frameworks_where_versions_diverge()
    {
        // net8.0: both projects resolve Serilog to 3.0.0 (agree — not a conflict there).
        // net10.0: ProjectOne=4.0.0, ProjectTwo=5.0.0 (diverge — a conflict).
        // Only the net10.0 versions should be reported; the agreed 3.0.0 must not appear.
        static string Project(string name, string netEight, string netTen) => AssetsFixture.Assets(
            name,
            new AssetsFixture.Framework(
                Tfm: "net8.0",
                DirectDependencies: $$""" "Serilog": { "target": "Package", "version": "[{{netEight}}, )" } """,
                Target: $$""" "Serilog/{{netEight}}": { "type": "package" } """),
            new AssetsFixture.Framework(
                Tfm: "net10.0",
                DirectDependencies: $$""" "Serilog": { "target": "Package", "version": "[{{netTen}}, )" } """,
                Target: $$""" "Serilog/{{netTen}}": { "type": "package" } """));

        var one = ProjectAssetsReader.Read(AssetsFixture.WriteAssets(Project("ProjectOne", "3.0.0", "4.0.0")));
        var two = ProjectAssetsReader.Read(AssetsFixture.WriteAssets(Project("ProjectTwo", "3.0.0", "5.0.0")));

        var conflict = new PackageAnalyzer()
            .Analyze(one.Concat(two).ToList())
            .Single(x => x.Kind == FindingKind.VersionConflict && x.PackageId == "Serilog");

        conflict.ConflictVersions.Select(x => x.Version).Should().BeEquivalentTo(new[] { "4.0.0", "5.0.0" });
        conflict.ResolvedVersion.Should().Be("5.0.0"); // the highest resolved version
    }

    [Fact]
    public void Restricts_analysis_to_the_requested_target_framework()
    {
        // The same redundancy (B via A) exists on both TFMs; asking for net10.0 yields net10.0 findings only.
        static AssetsFixture.Framework Redundant(string tfm) => new(
            Tfm: tfm,
            DirectDependencies: """
                                "A": { "target": "Package", "version": "[1.0.0, )" },
                                "B": { "target": "Package", "version": "[1.0.0, )" }
                                """,
            Target: """
                    "A/1.0.0": { "type": "package", "dependencies": { "B": "1.0.0" } },
                    "B/1.0.0": { "type": "package" }
                    """);

        var assets = AssetsFixture.WriteAssets(AssetsFixture.Assets("MultiTarget", Redundant("net8.0"), Redundant("net10.0")));
        var projects = ProjectAssetsReader.Read(assets);

        var options = new AnalyzerOptions { TargetFramework = "net10.0" };
        var findings = new PackageAnalyzer().Analyze(projects, options);

        findings.Should().NotBeEmpty();
        findings.Should().OnlyContain(x => x.TargetFramework == "net10.0");
        findings.Should().Contain(x => x.PackageId == "B");
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
        => AssetsFixture.SingleFramework(directDependencies, target, projectReferences, projectName, tfm);

    private static string WriteAssets(string json) => AssetsFixture.WriteAssets(json);
}
