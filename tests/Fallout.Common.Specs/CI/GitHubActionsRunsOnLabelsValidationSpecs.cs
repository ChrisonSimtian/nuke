using System;
using Fallout.Common.CI;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.Execution;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs.CI;

public class GitHubActionsRunsOnLabelsValidationSpecs
{
    [Fact]
    public void Matrix_with_runs_on_labels_throws()
    {
        var act = () => GetConfiguration(
            new[] { GitHubActionsImage.UbuntuLatest, GitHubActionsImage.WindowsLatest },
            new[] { "self-hosted", "linux", "x64" });

        act.Should().Throw<Exception>().WithMessage("*RunsOnLabels*");
    }

    [Fact]
    public void Single_image_with_runs_on_labels_does_not_throw()
    {
        var act = () => GetConfiguration(
            new[] { GitHubActionsImage.UbuntuLatest },
            new[] { "self-hosted", "linux", "x64" });

        act.Should().NotThrow();
    }

    [Fact]
    public void Matrix_without_runs_on_labels_does_not_throw()
    {
        var act = () => GetConfiguration(
            new[] { GitHubActionsImage.UbuntuLatest, GitHubActionsImage.WindowsLatest },
            new string[0]);

        act.Should().NotThrow();
    }

    [Fact]
    public void Single_label_does_not_throw()
    {
        var act = () => GetConfiguration(
            new[] { GitHubActionsImage.UbuntuLatest },
            new[] { "self-hosted" });

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_or_whitespace_label_element_throws(string badLabel)
    {
        var act = () => GetConfiguration(
            new[] { GitHubActionsImage.UbuntuLatest },
            new[] { "self-hosted", badLabel });

        act.Should().Throw<Exception>().WithMessage("*RunsOnLabels*");
    }

    private static void GetConfiguration(GitHubActionsImage[] images, string[] runsOnLabels)
    {
        var build = new ConfigurationGenerationSpecs.TestBuild();
        var relevantTargets = ExecutableTargetFactory.CreateAll(build, x => x.Compile);
        var attribute = new TestGitHubActionsAttribute(images[0], images[1..])
                        {
                            On = new[] { GitHubActionsTrigger.Push },
                            InvokedTargets = new[] { nameof(ConfigurationGenerationSpecs.TestBuild.Test) },
                            RunsOnLabels = runsOnLabels
                        };
        ((ConfigurationAttributeBase)attribute).Build = build;
        attribute.GetConfiguration(relevantTargets);
    }
}
