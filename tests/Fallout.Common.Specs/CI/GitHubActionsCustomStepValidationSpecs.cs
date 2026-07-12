using System;
using Fallout.Common.CI;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.CI.GitHubActions.Configuration;
using Fallout.Common.Execution;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs.CI;

public class GitHubActionsCustomStepValidationSpecs
{
    private sealed class InjectingBuild : ConfigurationGenerationSpecs.TestBuild, IConfigureGitHubActions
    {
        public Action<GitHubActionsStepPipeline> Hook { get; set; }
        public void ConfigureSteps(GitHubActionsStepPipeline pipeline) => Hook?.Invoke(pipeline);
    }

    private static Action Generate(params GitHubActionsCustomStep[] steps)
    {
        var build = new InjectingBuild
                    {
                        Hook = p =>
                        {
                            foreach (var step in steps)
                                p.Insert(GitHubActionsStepPosition.PostRun, step);
                        }
                    };
        var relevantTargets = ExecutableTargetFactory.CreateAll(build, x => x.Compile);

        var attribute = new TestGitHubActionsAttribute(GitHubActionsImage.UbuntuLatest)
                        {
                            On = new[] { GitHubActionsTrigger.Push },
                            InvokedTargets = new[] { nameof(ConfigurationGenerationSpecs.TestBuild.Test) }
                        };
        ((ConfigurationAttributeBase)attribute).Build = build;

        return () => attribute.GetConfiguration(relevantTargets);
    }

    [Fact]
    public void Step_with_both_uses_and_run_throws()
        => Generate(new GitHubActionsCustomStep { Uses = "a/b@v1", Run = new[] { "echo" } })
            .Should().Throw<ArgumentException>();

    [Fact]
    public void Step_with_neither_uses_nor_run_throws()
        => Generate(new GitHubActionsCustomStep { Name = "empty" })
            .Should().Throw<ArgumentException>();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Run_with_only_whitespace_entries_throws(string entry)
        => Generate(new GitHubActionsCustomStep { Run = new[] { entry } })
            .Should().Throw<ArgumentException>();

    [Fact]
    public void With_without_uses_throws()
        => Generate(new GitHubActionsCustomStep { Run = new[] { "echo" }, With = new() { ["k"] = "v" } })
            .Should().Throw<ArgumentException>();

    [Fact]
    public void Shell_on_a_uses_step_throws()
        => Generate(new GitHubActionsCustomStep { Uses = "a/b@v1", Shell = "pwsh" })
            .Should().Throw<ArgumentException>();

    [Fact]
    public void One_invalid_among_several_valid_throws()
        => Generate(
                new GitHubActionsCustomStep { Uses = "a/b@v1" },
                new GitHubActionsCustomStep { Name = "bad" },                 // neither uses nor run
                new GitHubActionsCustomStep { Run = new[] { "echo" } })
            .Should().Throw<ArgumentException>();

    [Fact]
    public void Well_formed_uses_step_does_not_throw()
        => Generate(new GitHubActionsCustomStep { Uses = "a/b@v1", With = new() { ["k"] = "v" } })
            .Should().NotThrow();

    [Fact]
    public void Well_formed_run_step_does_not_throw()
        => Generate(new GitHubActionsCustomStep { Run = new[] { "echo hi" }, Shell = "pwsh" })
            .Should().NotThrow();

    // Collections are publicly settable, so a caller can null them. That must be treated as empty, not crash.
    [Fact]
    public void Null_collections_on_a_uses_step_do_not_throw()
        => Generate(new GitHubActionsCustomStep { Uses = "a/b@v1", With = null, Env = null, Run = null })
            .Should().NotThrow();
}
