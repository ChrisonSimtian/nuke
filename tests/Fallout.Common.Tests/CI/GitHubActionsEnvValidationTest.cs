using System;
using Fallout.Common.CI;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.Execution;
using Fallout.Common.Tooling;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Tests.CI;

public class GitHubActionsEnvValidationTest
{
    [Theory]
    [InlineData(null)]                 // null entry — must be a clean ArgumentException, not an NRE
    [InlineData("")]                   // empty entry
    [InlineData("   ")]                // whitespace-only entry
    [InlineData("MISSING_COLON")]      // no separator at all
    [InlineData(": value")]            // empty key
    [InlineData("KEY WITH SPACE: 1")]  // whitespace inside the key
    [InlineData("KEY : value")]        // whitespace on the key (before the colon)
    [InlineData("KEY:value")]          // no space after the colon — invalid YAML mapping
    public void Malformed_env_entry_throws(string badEntry)
    {
        var act = () => GetConfiguration(new[] { badEntry });

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("DOTNET_NOLOGO: true")]
    [InlineData("Url: https://example.com")]  // first colon is the separator; colons in the value are fine
    [InlineData("EMPTY_VALUE:")]              // colon at end-of-entry — a valid empty (null) value
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
        var build = new ConfigurationGenerationTest.TestBuild();
        var relevantTargets = ExecutableTargetFactory.CreateAll(build, x => x.Compile);

        var attribute = new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                        {
                            On = new[] { GitHubActionsTrigger.Push },
                            InvokedTargets = new[] { nameof(ConfigurationGenerationTest.TestBuild.Test) },
                            Env = env
                        };
        ((ConfigurationAttributeBase)attribute).Build = build;

        attribute.GetConfiguration(relevantTargets);
    }
}
