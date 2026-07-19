using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Fallout.Migrate.Specs;

public class MigrationIntegrationSpecs
{
    [Fact]
    public async Task MigratesVanillaConsumerRepo()
    {
        var temp = CreateVanillaFixture();

        try
        {
            var migration = new Migration(temp, dryRun: false, TextWriter.Null);
            var summary = await migration.RunAsync();

            // Build file rewritten end to end.
            var buildCsproj = File.ReadAllText(Path.Combine(temp, "build", "_build.csproj"));
            buildCsproj.Should().Contain(@"Include=""Fallout.Common""");
            buildCsproj.Should().Contain("<FalloutRootDirectory>");
            buildCsproj.Should().NotContain("Nuke.Common");
            buildCsproj.Should().NotContain("<NukeRootDirectory>");

            var buildCs = File.ReadAllText(Path.Combine(temp, "build", "Build.cs"));
            buildCs.Should().Contain("using Fallout.Common");
            buildCs.Should().Contain(": FalloutBuild");
            buildCs.Should().NotContain("using Nuke.");
            buildCs.Should().NotContain("NukeBuild");

            var buildSh = File.ReadAllText(Path.Combine(temp, "build.sh"));
            buildSh.Should().Contain("dotnet fallout");
            buildSh.Should().Contain("FALLOUT_TELEMETRY_OPTOUT");
            buildSh.Should().Contain(".fallout/temp");

            // .nuke/ moved to .fallout/.
            Directory.Exists(Path.Combine(temp, ".nuke")).Should().BeFalse();
            Directory.Exists(Path.Combine(temp, ".fallout")).Should().BeTrue();
            File.Exists(Path.Combine(temp, ".fallout", "parameters.json")).Should().BeTrue();

            summary.FilesChanged.Should().BeGreaterThan(0);
            summary.EditCount.Should().BeGreaterThan(0);
            summary.DirectoriesRenamed.Should().Be(1);
            summary.Warnings.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public async Task DryRunDoesNotWriteFiles()
    {
        var temp = CreateVanillaFixture();

        try
        {
            var beforeCsproj = File.ReadAllText(Path.Combine(temp, "build", "_build.csproj"));
            var beforeNukeDir = Directory.Exists(Path.Combine(temp, ".nuke"));

            var summary = await new Migration(temp, dryRun: true, TextWriter.Null).RunAsync();

            File.ReadAllText(Path.Combine(temp, "build", "_build.csproj")).Should().Be(beforeCsproj);
            Directory.Exists(Path.Combine(temp, ".nuke")).Should().Be(beforeNukeDir);
            summary.FilesChanged.Should().BeGreaterThan(0); // counts intended edits
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public async Task WarnsWhenBothNukeAndFalloutDirectoriesExist()
    {
        var temp = CreateVanillaFixture();
        Directory.CreateDirectory(Path.Combine(temp, ".fallout"));

        try
        {
            var summary = await new Migration(temp, dryRun: false, TextWriter.Null).RunAsync();

            summary.Warnings.Should().Contain(w => w.Contains(".nuke/") && w.Contains(".fallout/"));
            summary.DirectoriesRenamed.Should().Be(0);
            Directory.Exists(Path.Combine(temp, ".nuke")).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void WarnsWhenBuildProjectTargetsOlderThanNet10()
    {
        var temp = CreateVanillaFixture();
        var buildCsprojPath = Path.Combine(temp, "build", "_build.csproj");
        File.WriteAllText(buildCsprojPath, File.ReadAllText(buildCsprojPath).Replace("net10.0", "net8.0"));

        try
        {
            var summary = new Migration(temp, dryRun: false, TextWriter.Null).Run();

            summary.Warnings.Should().Contain(w =>
                w.Contains("net8.0") && w.Contains(".NET 10") && w.Contains("_build.csproj"));
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void BumpsDotNetVersionAndSdk()
    {
        var temp = CreateVanillaFixture();
        var buildCsprojPath = Path.Combine(temp, "build", "_build.csproj");
        File.WriteAllText(buildCsprojPath, File.ReadAllText(buildCsprojPath).Replace("net10.0", "net8.0"));
        File.WriteAllText(Path.Combine(temp, "global.json"), """
                                                              {
                                                                "sdk": {
                                                                  "version": "8.0.100",
                                                                  "rollForward": "latestMinor"
                                                                }
                                                              }
                                                              """);

        try
        {
            new Migration(temp, dryRun: false, TextWriter.Null).Run();

            File.ReadAllText(buildCsprojPath).Should().Contain("<TargetFramework>net10.0</TargetFramework>");

            var globalJson = File.ReadAllText(Path.Combine(temp, "global.json"));
            globalJson.Should().Contain(@"""version"": ""10.0.100""");
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    private static string CreateVanillaFixture()
    {
        var dir = Path.Combine(Path.GetTempPath(), "fallout-migrate-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "build"));
        Directory.CreateDirectory(Path.Combine(dir, ".nuke"));

        File.WriteAllText(Path.Combine(dir, "build", "_build.csproj"), """
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

        File.WriteAllText(Path.Combine(dir, "build", "Build.cs"), """
                                                                  using Nuke.Common;
                                                                  using Nuke.Common.Tools.DotNet;

                                                                  class Build : NukeBuild
                                                                  {
                                                                      public static int Main () => Execute<Build>(x => x.Compile);

                                                                      Target Compile => _ => _.Executes(() => { });
                                                                  }
                                                                  """);

        File.WriteAllText(Path.Combine(dir, "build.sh"), """
                                                         #!/usr/bin/env bash
                                                         export NUKE_TELEMETRY_OPTOUT=1
                                                         TEMP_DIRECTORY="$SCRIPT_DIR/.nuke/temp"
                                                         dotnet nuke "$@"
                                                         """, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        File.WriteAllText(Path.Combine(dir, ".nuke", "parameters.json"), "{}");

        return dir;
    }
}
