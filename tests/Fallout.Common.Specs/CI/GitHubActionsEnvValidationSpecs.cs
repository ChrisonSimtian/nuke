using System;
using Fallout.Common.CI;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.Execution;
using Fallout.Common.Tooling;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs.CI;

public class GitHubActionsEnvValidationSpecs
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("MISSING_COLON")]
    [InlineData(": value")]
    [InlineData("KEY WITH SPACE: 1")]
    [InlineData("KEY : value")]
    [InlineData("KEY:value")]
    public void Malformed_env_entry_throws(string badEntry)
    {
        var act = () => GetConfiguration(new[] { badEntry });

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("DOTNET_NOLOGO: true")]
    [InlineData("Url: https://example.com")]
    [InlineData("EMPTY_VALUE:")]
    public void Well_formed_env_entry_does_not_throw(string goodEntry)
    {
        var act = () => GetConfiguration(new[] { goodEntry });

        act.Should().NotThrow();
    }

    [Fact]
    public void Malformed_entry_after_a_valid_one_still_throws()
    {
        var act = () => GetConfiguration(new[] { "GOOD: 1", "BAD" });

        act.Should().Throw<ArgumentException>();
    }

    private static void GetConfiguration(string[] env)
    {
        var build = new ConfigurationGenerationSpecs.TestBuild();
        var relevantTargets = ExecutableTargetFactory.CreateAll(build, x => x.Compile);

        var attribute = new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                        {
                            On = new[] { GitHubActionsTrigger.Push },
                            InvokedTargets = new[] { nameof(ConfigurationGenerationSpecs.TestBuild.Test) },
                            Env = env
                        };
        ((ConfigurationAttributeBase)attribute).Build = build;

        attribute.GetConfiguration(relevantTargets);
    }
}
