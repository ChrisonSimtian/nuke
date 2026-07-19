using FluentAssertions;
using Xunit;
using Fallout.Migrate.Steps;

namespace Fallout.Migrate.Specs;

public class BumpDotNetVersionStepSpecs
{
    [Fact]
    public void BumpsOlderTargetFrameworkToNet10()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <OutputType>Exe</OutputType>
                                 <TargetFramework>net8.0</TargetFramework>
                               </PropertyGroup>
                             </Project>
                             """;

        var result = BumpDotNetVersionStep.BumpTargetFramework(input);

        result.EditCount.Should().Be(1);
        result.Content.Should().Contain("<TargetFramework>net10.0</TargetFramework>");
        result.Content.Should().NotContain("net8.0");
    }

    [Fact]
    public void LeavesAlreadyNet10Unchanged()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <TargetFramework>net10.0</TargetFramework>
                               </PropertyGroup>
                             </Project>
                             """;

        var result = BumpDotNetVersionStep.BumpTargetFramework(input);

        result.EditCount.Should().Be(0);
        result.Content.Should().Be(input);
    }

    [Fact]
    public void LeavesNewerTargetFrameworkUnchanged()
    {
        const string input = """
                             <Project Sdk="Microsoft.NET.Sdk">
                               <PropertyGroup>
                                 <TargetFramework>net11.0</TargetFramework>
                               </PropertyGroup>
                             </Project>
                             """;

        var result = BumpDotNetVersionStep.BumpTargetFramework(input);

        result.EditCount.Should().Be(0);
        result.Content.Should().Be(input);
    }

    [Fact]
    public void BumpsSdkVersionInGlobalJson()
    {
        const string input = """
                             {
                               "sdk": {
                                 "version": "8.0.100",
                                 "rollForward": "latestMinor"
                               }
                             }
                             """;

        var result = BumpDotNetVersionStep.BumpSdkVersion(input);

        result.EditCount.Should().Be(1);
        result.Content.Should().Contain(@"""version"": ""10.0.100""");
        result.Content.Should().Contain(@"""rollForward"": ""latestMinor""");
        result.Content.Should().NotContain("8.0.100");
    }

    [Fact]
    public void LeavesAlreadyPinnedSdkVersionUnchanged()
    {
        const string input = """
                             {
                               "sdk": {
                                 "version": "10.0.100"
                               }
                             }
                             """;

        var result = BumpDotNetVersionStep.BumpSdkVersion(input);

        result.EditCount.Should().Be(0);
        result.Content.Should().Be(input);
    }

    [Fact]
    public void LeavesNewerSdkVersionUnchanged()
    {
        const string input = """
                             {
                               "sdk": {
                                 "version": "11.0.100"
                               }
                             }
                             """;

        var result = BumpDotNetVersionStep.BumpSdkVersion(input);

        result.EditCount.Should().Be(0);
        result.Content.Should().Be(input);
    }
}
