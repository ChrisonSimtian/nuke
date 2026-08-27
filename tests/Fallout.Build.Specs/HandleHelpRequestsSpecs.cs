using System.Collections.Generic;
using System.Linq;
using Fallout.Build.Execution.Extensions;
using Fallout.Common.Execution;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs.Execution;

/// <summary>
/// Characterization tests for the <c>--help</c> target listing. #642 re-points this text at the same
/// <see cref="BuildGraphUtility" /> projection the machine-readable documents use, so the human and
/// machine views cannot drift; these assertions pin the rendered output across that refactor.
/// </summary>
public class HandleHelpRequestsSpecs
{
    [Fact]
    public void Help_lists_every_listed_target_and_hides_the_unlisted_ones()
    {
        var text = TargetsTextFor(SampleGraph());

        text.Should().Contain("Restore").And.Contain("Compile").And.Contain("Test");
        text.Should().NotContain("Publish", "unlisted targets are not part of the help listing");
    }

    [Fact]
    public void Help_marks_the_default_target_and_renders_direct_dependencies()
    {
        var text = TargetsTextFor(SampleGraph());

        text.Should().Contain("Test (default)");
        text.Should().Contain("-> Restore");
    }

    [Fact]
    public void Help_renders_a_target_description_underneath_its_entry()
    {
        TargetsTextFor(SampleGraph()).Should().Contain("Builds all projects");
    }

    [Fact]
    public void Help_renders_the_same_listed_set_the_model_reports()
    {
        var graph = SampleGraph();
        var text = TargetsTextFor(graph);

        var listed = BuildGraphUtility.GetModel(graph, falloutVersion: null)
            .Targets.Where(x => x.Listed).Select(x => x.Name);

        foreach (var name in listed)
            text.Should().Contain(name);
    }

    private static string TargetsTextFor(IReadOnlyCollection<ExecutableTarget> graph)
        => new HandleHelpRequestsAttribute { Build = new SampleBuild { ExecutableTargets = graph } }
            .GetTargetsText();

    private static IReadOnlyCollection<ExecutableTarget> SampleGraph()
    {
        var restore = new ExecutableTarget { Name = "Restore", Listed = true };
        var compile = new ExecutableTarget
                      {
                          Name = "Compile",
                          Description = "Builds all projects",
                          Listed = true,
                      };
        var test = new ExecutableTarget { Name = "Test", Listed = true, IsDefault = true };
        var publish = new ExecutableTarget { Name = "Publish", Listed = false };

        compile.ExecutionDependencies.Add(restore);
        test.ExecutionDependencies.Add(restore);

        return new[] { restore, compile, test, publish };
    }

    private class SampleBuild : FalloutBuild
    {
    }
}
