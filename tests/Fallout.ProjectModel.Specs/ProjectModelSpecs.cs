using System;
using System.Linq;
using FluentAssertions;
using Fallout.Common.IO;
using Fallout.Solutions;
using Xunit;

namespace Fallout.Common.Specs;

public class ProjectModelSpecs
{
    private static AbsolutePath RootDirectory => Constants.TryGetRootDirectoryFrom(EnvironmentInfo.WorkingDirectory).NotNull();

    private static AbsolutePath SolutionFile => RootDirectory / "fallout.slnx";

    [Fact]
    public void ProjectSpec()
    {
        var solution = SolutionFile.ReadSolution();
        var project = solution.Projects.Single(x => x.Name == "Fallout.ProjectModel");

        var action = new Action(() => project.GetMSBuildProject());
        action.Should().NotThrow();

        project.GetTargetFrameworks().Should().Equal("net8.0", "net9.0", "net10.0");
        project.HasPackageReference("Microsoft.Build.Locator").Should().BeTrue();
        project.GetPackageReferenceVersion("Microsoft.Build.Locator").Should().Be("1.7.8");
        project.GetPackageReferenceVersion("Microsoft.Build").Should().Be("18.0.2");
    }

    [Fact]
    public void MSBuildProjectSpec()
    {
        var solution = SolutionFile.ReadSolution();
        var project = solution.Projects.Single(x => x.Name == "Fallout.ProjectModel");

        var msbuildProject = project.GetMSBuildProject(targetFramework: "net8.0");

        var package = msbuildProject.GetItems("PackageVersion").FirstOrDefault(x => x.EvaluatedInclude == "Microsoft.Build");
        package.Should().NotBeNull();
        package.GetMetadataValue("Version").Should().Be("17.11.48");
    }
}
