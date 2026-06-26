using System;
using Fallout.Common.CI;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.Execution;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs.CI;

public class GitHubActionsInputValidationSpecs
{
    [Theory]
    [InlineData(GitHubActionsInputType.Choice, null, new string[0])]     // choice without options
    [InlineData(GitHubActionsInputType.Choice, "x", new[] { "a", "b" })] // default not in options
    [InlineData(GitHubActionsInputType.String, null, new[] { "a" })]     // options on a non-choice input
    [InlineData(GitHubActionsInputType.Number, "abc", new string[0])]    // non-numeric number default
    [InlineData(GitHubActionsInputType.Boolean, "yes", new string[0])]   // non-boolean boolean default
    public void Malformed_input_throws(GitHubActionsInputType type, string @default, string[] options)
    {
        var act = () => GetConfiguration(
            new GitHubActionsInputAttribute("Input") { Type = type, Default = @default, Options = options });

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(GitHubActionsInputType.Choice, "a", new[] { "a", "b" })]
    [InlineData(GitHubActionsInputType.Number, "1.5", new string[0])]
    [InlineData(GitHubActionsInputType.Boolean, "true", new string[0])]
    [InlineData(GitHubActionsInputType.Environment, null, new string[0])]
    [InlineData(GitHubActionsInputType.String, null, new string[0])]
    public void Well_formed_input_does_not_throw(GitHubActionsInputType type, string @default, string[] options)
    {
        var act = () => GetConfiguration(
            new GitHubActionsInputAttribute("Input") { Type = type, Default = @default, Options = options });

        act.Should().NotThrow();
    }

    [Fact]
    public void Unknown_workflow_name_throws()
    {
        var act = () => GetConfiguration(
            new GitHubActionsInputAttribute("Input") { Workflows = new[] { "does-not-exist" } });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Duplicate_input_name_throws()
    {
        var act = () => GetConfiguration(
            new GitHubActionsInputAttribute("Dup"),
            new GitHubActionsInputAttribute("Dup"));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_input_name_throws(string name)
    {
        var act = () => GetConfiguration(new GitHubActionsInputAttribute(name));

        act.Should().Throw<ArgumentException>();
    }

    private static void GetConfiguration(params GitHubActionsInputAttribute[] inputs)
    {
        var build = new ConfigurationGenerationSpecs.TestBuild();
        var relevantTargets = ExecutableTargetFactory.CreateAll(build, x => x.Compile);

        // No shorthand On: the typed inputs alone drive the detailed workflow_dispatch trigger, so
        // ShortTriggers stays empty and the short-XOR-detailed assert does not interfere.
        var attribute = new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                        {
                            InvokedTargets = new[] { nameof(ConfigurationGenerationSpecs.TestBuild.Test) },
                            Inputs = inputs
                        };
        ((ConfigurationAttributeBase)attribute).Build = build;

        attribute.GetConfiguration(relevantTargets);
    }
}
