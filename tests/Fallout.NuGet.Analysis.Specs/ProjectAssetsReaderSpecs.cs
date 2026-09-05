using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Fallout.NuGet.Analysis.Specs;

public sealed class ProjectAssetsReaderSpecs
{
    [Fact]
    public void Reads_one_analyzed_project_per_target_framework()
    {
        var assets = AssetsFixture.WriteAssets(AssetsFixture.Assets(
            "MultiTarget",
            new AssetsFixture.Framework(
                Tfm: "net8.0",
                DirectDependencies: """ "A": { "target": "Package", "version": "[1.0.0, )" } """,
                Target: """ "A/1.0.0": { "type": "package" } """),
            new AssetsFixture.Framework(
                Tfm: "net10.0",
                DirectDependencies: """ "A": { "target": "Package", "version": "[2.0.0, )" } """,
                Target: """ "A/2.0.0": { "type": "package" } """)));

        var projects = ProjectAssetsReader.Read(assets);

        projects.Select(x => x.TargetFramework).Should().BeEquivalentTo(new[] { "net8.0", "net10.0" });
        projects.Single(x => x.TargetFramework == "net10.0").Graph.Values
            .Should().Contain(x => x.Name == "A" && x.Version == "2.0.0");
    }

    [Fact]
    public void Skips_rid_qualified_targets()
    {
        // A "tfm/rid" target carries RID-specific nodes that the analyzer must not treat as resolved deps.
        var assets = AssetsFixture.WriteAssets(AssetsFixture.Assets(
            "App",
            extraTargets: """
                          "net10.0/win-x64": {
                              "RidOnly/9.9.9": { "type": "package" }
                          }
                          """,
            new AssetsFixture.Framework(
                Tfm: "net10.0",
                DirectDependencies: """ "A": { "target": "Package", "version": "[1.0.0, )" } """,
                Target: """ "A/1.0.0": { "type": "package" } """)));

        var project = ProjectAssetsReader.Read(assets).Single();

        project.Graph.Values.Should().Contain(x => x.Name == "A");
        project.Graph.Values.Should().NotContain(x => x.Name == "RidOnly");
    }

    [Fact]
    public void Prefers_the_target_alias_over_the_framework_key()
    {
        var assets = AssetsFixture.WriteAssets(AssetsFixture.Assets(
            "Aliased",
            new AssetsFixture.Framework(
                Tfm: "net10.0",
                DirectDependencies: """ "A": { "target": "Package", "version": "[1.0.0, )" } """,
                Target: """ "A/1.0.0": { "type": "package" } """,
                Alias: "custom-tfm")));

        ProjectAssetsReader.Read(assets).Single().TargetFramework.Should().Be("custom-tfm");
    }

    [Fact]
    public void Returns_nothing_for_an_assets_file_without_a_project_section()
    {
        var assets = AssetsFixture.WriteAssets(""" { "version": 3, "targets": {} } """);

        ProjectAssetsReader.Read(assets).Should().BeEmpty();
    }

    [Fact]
    public void Returns_nothing_when_the_project_declares_no_frameworks()
    {
        var assets = AssetsFixture.WriteAssets(
            """ { "version": 3, "targets": {}, "project": { "restore": { "projectName": "X" } } } """);

        ProjectAssetsReader.Read(assets).Should().BeEmpty();
    }

    [Fact]
    public void FindAssetsFile_returns_the_path_when_the_project_has_been_restored()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"proj-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(directory, "obj"));
        var assets = Path.Combine(directory, "obj", "project.assets.json");
        File.WriteAllText(assets, "{}");

        ProjectAssetsReader.FindAssetsFile(Path.Combine(directory, "My.csproj")).Should().Be(assets);
    }

    [Fact]
    public void FindAssetsFile_returns_null_when_the_project_is_not_restored()
    {
        var projectFile = Path.Combine(Path.GetTempPath(), $"proj-{Guid.NewGuid():N}", "My.csproj");

        ProjectAssetsReader.FindAssetsFile(projectFile).Should().BeNull();
    }
}
