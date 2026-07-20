using System;
using System.IO;
using System.Threading.Tasks;
using Fallout.Common.IO;
using FluentAssertions;
using Xunit;

namespace Fallout.Migrate.Specs;

public class MigrationIntegrationSpecs : IDisposable
{
    private readonly AbsolutePath tempDirectory;

    public MigrationIntegrationSpecs()
    {
        // Arrange
        tempDirectory = AbsolutePath.Temp("fallout-migrate-test");
        (tempDirectory / "build").CreateDirectory();
        (tempDirectory / ".nuke").CreateDirectory();

        (tempDirectory / "build" / "_build.csproj").WriteAllText(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <NukeRootDirectory>.\..</NukeRootDirectory>
                <NukeTelemetryVersion>1</NukeTelemetryVersion>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Nuke.Common" Version="9.0.0" />
              </ItemGroup>
            </Project>
            """);

        (tempDirectory / "build" / "Build.cs").WriteAllText(
            """
            using Nuke.Common;
            using Nuke.Common.Tools.DotNet;

            class Build : NukeBuild
            {
                public static int Main () => Execute<Build>(x => x.Compile);

                Target Compile => _ => _.Executes(() => { });
            }
            """);

        (tempDirectory / "build.sh").WriteAllText(
            """
            #!/usr/bin/env bash
            export NUKE_TELEMETRY_OPTOUT=1
            TEMP_DIRECTORY="$SCRIPT_DIR/.nuke/temp"
            dotnet nuke "$@"
            """);

        (tempDirectory / ".nuke" / "parameters.json").WriteAllText("{}");
    }

    public void Dispose()
    {
        tempDirectory.DeleteDirectory();
    }

    [Fact]
    public async Task A_vanilla_consumer_repo_is_migrated_end_to_end()
    {
        // Act
        var migration = new Migration(tempDirectory, dryRun: false, TextWriter.Null);
        var summary = await migration.RunAsync();

        // Assert

        // Build file rewritten end to end.
        var buildCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();
        buildCsproj.Should().Contain(@"Include=""Fallout.Common""");
        buildCsproj.Should().Contain("<FalloutRootDirectory>");
        buildCsproj.Should().NotContain("Nuke.Common");
        buildCsproj.Should().NotContain("<NukeRootDirectory>");

        var buildCs = (tempDirectory / "build" / "Build.cs").ReadAllText();
        buildCs.Should().Contain("using Fallout.Common");
        buildCs.Should().Contain(": FalloutBuild");
        buildCs.Should().NotContain("using Nuke.");
        buildCs.Should().NotContain("NukeBuild");

        var buildSh = (tempDirectory / "build.sh").ReadAllText();
        buildSh.Should().Contain("dotnet fallout");
        buildSh.Should().NotContain("TELEMETRY_OPTOUT"); // telemetry removed — opt-out stripped, not renamed (ADR-0010)
        buildSh.Should().Contain(".fallout/temp");
        buildSh.Should().NotContain("if [[ ! -z ${NUKE_ENTERPRISE_TOKEN+x} && \"$NUKE_ENTERPRISE_TOKEN\" != \"\" ]]; then");

        // .nuke/ moved to .fallout/.
        (tempDirectory / ".nuke").DirectoryExists().Should().BeFalse();
        (tempDirectory / ".fallout").DirectoryExists().Should().BeTrue();
        (tempDirectory / ".fallout" / "parameters.json").FileExists().Should().BeTrue();

        summary.FilesChanged.Should().BeGreaterThan(0);
        summary.EditCount.Should().BeGreaterThan(0);
        summary.DirectoriesRenamed.Should().Be(1);
        summary.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task A_dry_run_does_not_write_any_files()
    {
        // Arrange
        var beforeCsproj = (tempDirectory / "build" / "_build.csproj").ReadAllText();
        var beforeNukeDir = (tempDirectory / ".nuke").DirectoryExists();

        // Act
        var summary = await new Migration(tempDirectory, dryRun: true, TextWriter.Null).RunAsync();

        // Assert
        (tempDirectory / "build" / "_build.csproj").ReadAllText().Should().Be(beforeCsproj);
        (tempDirectory / ".nuke").DirectoryExists().Should().Be(beforeNukeDir);
        summary.FilesChanged.Should().BeGreaterThan(0); // counts intended edits
    }

    [Fact]
    public async Task Nuke_and_fallout_directories_coexisting_produces_a_warning()
    {
        // Arrange
        (tempDirectory / ".fallout").CreateDirectory();

        // Act
        var summary = await new Migration(tempDirectory, dryRun: false, TextWriter.Null).RunAsync();

        // Assert
        summary.Warnings.Should().Contain(w => w.Contains(".nuke/") && w.Contains(".fallout/"));
        summary.DirectoriesRenamed.Should().Be(0);
        (tempDirectory / ".nuke").DirectoryExists().Should().BeTrue();
    }

    [Fact]
    public async Task A_build_project_targeting_an_older_tfm_than_net10_produces_a_warning()
    {
        // Arrange
        var buildCsprojPath = tempDirectory / "build" / "_build.csproj";
        buildCsprojPath.UpdateText(text => text.Replace("net10.0", "net8.0"));

        // Act
        var summary = await new Migration(tempDirectory, dryRun: false, TextWriter.Null).RunAsync();

        // Assert
        summary.Warnings.Should().Contain(w =>
            w.Contains("net8.0") && w.Contains(".NET 10") && w.Contains("_build.csproj"));
    }

    [Fact]
    public async Task Migration_bumps_the_target_framework_and_global_json_sdk_version()
    {
        // Arrange
        var buildCsprojPath = tempDirectory / "build" / "_build.csproj";
        buildCsprojPath.UpdateText(text => text.Replace("net10.0", "net8.0"));
        (tempDirectory / "global.json").WriteAllText(
            """
            {
              "sdk": {
                "version": "8.0.100",
                "rollForward": "latestMinor"
              }
            }
            """);

        // Act
        await new Migration(tempDirectory, dryRun: false, TextWriter.Null).RunAsync();

        // Assert
        buildCsprojPath.ReadAllText().Should().Contain("<TargetFramework>net10.0</TargetFramework>");

        var globalJson = (tempDirectory / "global.json").ReadAllText();
        globalJson.Should().Contain(@"""version"": ""10.0.100""");
    }
}
