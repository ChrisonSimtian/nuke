using System;
using System.Linq;
using FluentAssertions;
using Fallout.Common.IO;
using Fallout.Solutions;
using Fallout.Common.Utilities;
using Xunit;

namespace Fallout.Common.Specs;

public class SolutionModelSpecs
{
    private static AbsolutePath RootDirectory => Constants.TryGetRootDirectoryFrom(EnvironmentInfo.WorkingDirectory).NotNull();

    private static AbsolutePath SolutionFile => RootDirectory / "fallout.slnx";

    [Fact]
    public void SolutionSpec()
    {
        var solution = SolutionFile.ReadSolution();

        solution.SolutionFolders.Select(x => x.Name).Should().BeEquivalentTo("misc", "01-test");

        var buildProject = solution.AllProjects.SingleOrDefault(x => x.Name == "_build");
        buildProject.Should().NotBeNull();

        // solution.SaveAs(solution.Path + ".bak");
    }

    [Fact]
    public void SolutionGetProjectsSpec()
    {
        var solution = SolutionFile.ReadSolution();

        solution.GetAllProjects("*.Specs").Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
