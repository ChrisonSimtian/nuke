using System;
using System.Linq;
using FluentAssertions;
using Fallout.Common.IO;
using Fallout.Solutions;
using Fallout.Common.Utilities;
using Xunit;

namespace Fallout.Common.Tests;

public class SolutionModelTest
{
    private static AbsolutePath RootDirectory => Constants.TryGetRootDirectoryFrom(EnvironmentInfo.WorkingDirectory).NotNull();

    private static AbsolutePath SolutionFile => RootDirectory / "fallout.slnx";

    [Fact]
    public void SolutionTest()
    {
        var solution = SolutionFile.ReadSolution();

        solution.SolutionFolders.Select(x => x.Name).Should().BeEquivalentTo("misc");

        var buildProject = solution.AllProjects.SingleOrDefault(x => x.Name == "_build");
        buildProject.Should().NotBeNull();

        // solution.SaveAs(solution.Path + ".bak");
    }

    [Fact]
    public void SolutionGetProjectsTest()
    {
        var solution = SolutionFile.ReadSolution();

        solution.GetAllProjects("*.Tests").Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
