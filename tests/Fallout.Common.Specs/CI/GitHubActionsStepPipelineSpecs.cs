using System.Linq;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.CI.GitHubActions.Configuration;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs.CI;

public class GitHubActionsStepPipelineSpecs
{
    private static GitHubActionsStepPipeline NewPipeline()
        => new GitHubActionsStepPipeline("build", GitHubActionsImage.UbuntuLatest,
            new GitHubActionsStep[] { new GitHubActionsCustomStep { Uses = "actions/checkout@v6" } });

    [Fact]
    public void Context_is_exposed()
    {
        var pipeline = NewPipeline();

        pipeline.WorkflowName.Should().Be("build");
        pipeline.Image.Should().Be(GitHubActionsImage.UbuntuLatest);
        pipeline.BuiltInSteps.Should().HaveCount(1);
    }

    [Fact]
    public void Inserts_at_one_position_preserve_call_order()
    {
        var pipeline = NewPipeline();
        pipeline.Insert(GitHubActionsStepPosition.PostRun, new GitHubActionsCustomStep { Name = "first", Run = new[] { "a" } });
        pipeline.Insert(GitHubActionsStepPosition.PostRun, new GitHubActionsCustomStep { Name = "second", Run = new[] { "b" } });

        pipeline.GetInserts(GitHubActionsStepPosition.PostRun).Select(x => x.Name)
            .Should().Equal("first", "second");
    }

    [Fact]
    public void Insert_range_appends_in_order()
    {
        var pipeline = NewPipeline();
        pipeline.Insert(GitHubActionsStepPosition.PreRun, new[]
                        {
                            new GitHubActionsCustomStep { Name = "x", Run = new[] { "a" } },
                            new GitHubActionsCustomStep { Name = "y", Run = new[] { "b" } }
                        });

        pipeline.GetInserts(GitHubActionsStepPosition.PreRun).Select(x => x.Name)
            .Should().Equal("x", "y");
    }

    [Fact]
    public void Positions_without_inserts_are_empty()
    {
        var pipeline = NewPipeline();

        pipeline.GetInserts(GitHubActionsStepPosition.JobEnd).Should().BeEmpty();
    }
}
