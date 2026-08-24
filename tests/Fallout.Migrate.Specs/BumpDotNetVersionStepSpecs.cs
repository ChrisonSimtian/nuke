using System;
using System.IO;
using System.Threading.Tasks;
using Fallout.Common.IO;
using Fallout.Migrate.Common;
using Fallout.Migrate.Steps;
using FluentAssertions;
using Xunit;

namespace Fallout.Migrate.Specs;

public class BumpDotNetVersionStepSpecs : IDisposable
{
    private readonly AbsolutePath tempDirectory;
    private readonly MigrationContext context;
    private readonly Summary summary = new();

    public BumpDotNetVersionStepSpecs()
    {
        // Arrange
        tempDirectory = AbsolutePath.Temp("fallout-bump-dotnet-version");
        context = new MigrationContext(tempDirectory, dryRun: false, TextWriter.Null);
    }

    [Fact]
    public async Task Older_target_framework_is_bumped_to_net10()
    {
        // Arrange
        var buildCsproj = tempDirectory / "build" / "_build.csproj";
        buildCsproj.WriteAllText(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        // Act
        await new BumpDotNetVersionStep().ExecuteAsync(context, summary);

        // Assert
        var content = buildCsproj.ReadAllText();
        content.Should().Contain("<TargetFramework>net10.0</TargetFramework>");
        content.Should().NotContain("net8.0");
        summary.FilesChanged.Should().Be(1);
        summary.EditCount.Should().Be(1);
    }

    [Fact]
    public async Task Already_net10_target_framework_is_left_unchanged()
    {
        // Arrange
        var buildCsproj = tempDirectory / "build" / "_build.csproj";
        const string original =
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;

        buildCsproj.WriteAllText(original);
        var before = buildCsproj.ReadAllText();

        // Act
        await new BumpDotNetVersionStep().ExecuteAsync(context, summary);

        // Assert
        buildCsproj.ReadAllText().Should().Be(before);
        summary.FilesChanged.Should().Be(0);
        summary.EditCount.Should().Be(0);
    }

    [Fact]
    public async Task Newer_target_framework_is_left_unchanged()
    {
        // Arrange
        var buildCsproj = tempDirectory / "build" / "_build.csproj";
        const string original =
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net11.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;

        buildCsproj.WriteAllText(original);
        var before = buildCsproj.ReadAllText();

        // Act
        await new BumpDotNetVersionStep().ExecuteAsync(context, summary);

        // Assert
        buildCsproj.ReadAllText().Should().Be(before);
        summary.FilesChanged.Should().Be(0);
        summary.EditCount.Should().Be(0);
    }

    [Fact]
    public async Task Old_sdk_version_in_global_json_is_bumped()
    {
        // Arrange
        var globalJson = tempDirectory / "global.json";
        globalJson.WriteAllText(
            """
            {
              "sdk": {
                "version": "8.0.100",
                "rollForward": "latestMinor"
              }
            }
            """);

        // Act
        await new BumpDotNetVersionStep().ExecuteAsync(context, summary);

        // Assert
        var content = globalJson.ReadAllText();
        content.Should().Contain(@"""version"": ""10.0.100""");
        content.Should().Contain(@"""rollForward"": ""latestMinor""");
        content.Should().NotContain("8.0.100");
        summary.FilesChanged.Should().Be(1);
        summary.EditCount.Should().Be(1);
    }

    [Fact]
    public async Task Already_pinned_sdk_version_is_left_unchanged()
    {
        // Arrange
        var globalJson = tempDirectory / "global.json";
        const string original =
            """
            {
              "sdk": {
                "version": "10.0.100"
              }
            }
            """;

        globalJson.WriteAllText(original);
        var before = globalJson.ReadAllText();

        // Act
        await new BumpDotNetVersionStep().ExecuteAsync(context, summary);

        // Assert
        globalJson.ReadAllText().Should().Be(before);
        summary.FilesChanged.Should().Be(0);
        summary.EditCount.Should().Be(0);
    }

    [Fact]
    public async Task Newer_sdk_version_is_left_unchanged()
    {
        // Arrange
        var globalJson = tempDirectory / "global.json";
        const string original =
            """
            {
              "sdk": {
                "version": "11.0.100"
              }
            }
            """;

        globalJson.WriteAllText(original);
        var before = globalJson.ReadAllText();

        // Act
        await new BumpDotNetVersionStep().ExecuteAsync(context, summary);

        // Assert
        globalJson.ReadAllText().Should().Be(before);
        summary.FilesChanged.Should().Be(0);
        summary.EditCount.Should().Be(0);
    }

    public void Dispose()
    {
        tempDirectory.DeleteDirectory();
    }
}
