using System;
using System.IO;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using FluentAssertions;
using Xunit;

namespace Fallout.Cli.Specs;

public class BuildProjectResolverSpecs
{
    [Fact]
    public void Resolve_ConventionDefault_ReturnsBuildSlashUnderscoreBuildCsproj()
    {
        using var root = TempRepoRoot.Create();
        root.WriteParametersFile(parametersJson: "{}");
        var expected = root.WriteBuildProjectAtConvention();

        var actual = BuildProjectResolver.Resolve(root.Path);

        actual.Should().Be(expected);
    }

    [Fact]
    public void Resolve_WithExplicitBuildProjectFile_ReturnsConfiguredPath()
    {
        using var root = TempRepoRoot.Create();
        var customDir = root.Path / "custom";
        customDir.CreateDirectory();
        var customProject = customDir / "my-build.csproj";
        File.WriteAllText(customProject, string.Empty);
        root.WriteParametersFile(parametersJson: """{ "BuildProjectFile": "custom/my-build.csproj" }""");

        var actual = BuildProjectResolver.Resolve(root.Path);

        actual.Should().Be(customProject);
    }

    [Fact]
    public void Resolve_NoParametersFile_FallsBackToConvention()
    {
        using var root = TempRepoRoot.Create();
        // .fallout/ exists (TempRepoRoot creates it) but no parameters.json.
        var expected = root.WriteBuildProjectAtConvention();

        var actual = BuildProjectResolver.Resolve(root.Path);

        actual.Should().Be(expected);
    }

    [Fact]
    public void Resolve_EmptyBuildProjectFileValue_FallsBackToConvention()
    {
        using var root = TempRepoRoot.Create();
        root.WriteParametersFile(parametersJson: """{ "BuildProjectFile": "" }""");
        var expected = root.WriteBuildProjectAtConvention();

        var actual = BuildProjectResolver.Resolve(root.Path);

        actual.Should().Be(expected);
    }

    [Fact]
    public void Resolve_MissingBuildProjectAtConvention_Throws()
    {
        using var root = TempRepoRoot.Create();
        root.WriteParametersFile(parametersJson: "{}");
        // No build/_build.csproj — should throw.

        var action = () => BuildProjectResolver.Resolve(root.Path);

        action.Should().Throw<Exception>()
            .WithMessage("*Could not locate build project*");
    }

    [Fact]
    public void Resolve_MissingConfiguredBuildProjectFile_Throws()
    {
        using var root = TempRepoRoot.Create();
        root.WriteParametersFile(parametersJson: """{ "BuildProjectFile": "missing/nowhere.csproj" }""");

        var action = () => BuildProjectResolver.Resolve(root.Path);

        action.Should().Throw<Exception>()
            .WithMessage("*BuildProjectFile*does not exist*");
    }

    private sealed class TempRepoRoot : IDisposable
    {
        public AbsolutePath Path { get; }

        private TempRepoRoot(AbsolutePath path)
        {
            Path = path;
            (path / ".fallout").CreateDirectory();
        }

        public static TempRepoRoot Create()
        {
            var dir = (AbsolutePath)System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fallout-test-" + Guid.NewGuid().ToString("N"));
            dir.CreateDirectory();
            return new TempRepoRoot(dir);
        }

        public void WriteParametersFile(string parametersJson)
            => File.WriteAllText(Path / ".fallout" / "parameters.json", parametersJson);

        public AbsolutePath WriteBuildProjectAtConvention()
        {
            var buildDir = Path / "build";
            buildDir.CreateDirectory();
            var projectFile = buildDir / "_build.csproj";
            File.WriteAllText(projectFile, string.Empty);
            return projectFile;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
