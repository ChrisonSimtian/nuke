using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Fallout.Common.IO;
using Fallout.SolutionCodegen;
using Xunit;

namespace Fallout.Solution.Codegen.Specs;

public class SolutionCodegenRunnerSpecs : IDisposable
{
    private readonly AbsolutePath _root;
    private readonly string _outDir;

    public SolutionCodegenRunnerSpecs()
    {
        _root = (AbsolutePath)Path.Combine(Path.GetTempPath(), "fallout-codegen-" + Path.GetRandomFileName());
        _outDir = Path.Combine(_root, "out");
        Directory.CreateDirectory(_outDir);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Emits_accessor_for_discovered_member_from_the_real_solution()
    {
        WriteBuild("""[Solution("app.slnx", GenerateProjects = true)] readonly Solution Solution;""");
        WriteSolution("app.slnx", "src/Alpha/Alpha.csproj", "src/Beta/Beta.csproj");

        var generated = SolutionCodegenRunner.Run(_root, _outDir, new[] { (_root / "Build.cs").ToString() });

        generated.Select(x => x.FileName).Should().Equal("Solution.g.cs");
        var emitted = File.ReadAllText(Path.Combine(_outDir, "Solution.g.cs"));
        emitted.Should().Contain("GetProject(\"Alpha\")").And.Contain("GetProject(\"Beta\")");
    }

    [Fact]
    public void Clears_stale_outputs_before_emitting()
    {
        // a *.g.cs left by a previous run whose [Solution] member has since been removed/renamed
        File.WriteAllText(Path.Combine(_outDir, "Removed.g.cs"), "// orphan");
        WriteBuild("""[Solution("app.slnx", GenerateProjects = true)] readonly Solution Solution;""");
        WriteSolution("app.slnx", "src/Alpha/Alpha.csproj");

        SolutionCodegenRunner.Run(_root, _outDir, new[] { (_root / "Build.cs").ToString() });

        File.Exists(Path.Combine(_outDir, "Removed.g.cs")).Should().BeFalse();
        File.Exists(Path.Combine(_outDir, "Solution.g.cs")).Should().BeTrue();
    }

    [Fact]
    public void Removes_all_outputs_when_no_member_remains()
    {
        File.WriteAllText(Path.Combine(_outDir, "Solution.g.cs"), "// stale");
        WriteBuild("readonly Solution Solution;"); // no [Solution] attribute anymore

        var generated = SolutionCodegenRunner.Run(_root, _outDir, new[] { (_root / "Build.cs").ToString() });

        generated.Should().BeEmpty();
        Directory.EnumerateFiles(_outDir, "*.g.cs").Should().BeEmpty();
    }

    [Fact]
    public void Resolves_solution_from_parameters_file_when_no_path_given()
    {
        // No path on the attribute -> the solution is looked up in .fallout/parameters.json by member name.
        Directory.CreateDirectory(_root / ".fallout");
        File.WriteAllText(_root / ".fallout" / "parameters.json", """{ "Solution": "app.slnx" }""");
        WriteSolution("app.slnx", "src/Alpha/Alpha.csproj");
        WriteBuild("[Solution(GenerateProjects = true)] readonly Solution Solution;");

        var generated = SolutionCodegenRunner.Run(_root, _outDir, new[] { (_root / "Build.cs").ToString() });

        generated.Select(x => x.FileName).Should().Equal("Solution.g.cs");
        File.ReadAllText(Path.Combine(_outDir, "Solution.g.cs")).Should().Contain("GetProject(\"Alpha\")");
    }

    [Fact]
    public void Emits_one_file_per_member_for_multiple_solutions()
    {
        WriteBuild("""
                   [Solution("a.slnx", GenerateProjects = true)] readonly Solution First;
                       [Solution("b.slnx", GenerateProjects = true)] readonly Solution Second;
                   """);
        WriteSolution("a.slnx", "src/Alpha/Alpha.csproj");
        WriteSolution("b.slnx", "src/Beta/Beta.csproj");

        var generated = SolutionCodegenRunner.Run(_root, _outDir, new[] { (_root / "Build.cs").ToString() });

        generated.Select(x => x.FileName).Should().BeEquivalentTo("First.g.cs", "Second.g.cs");
        File.ReadAllText(Path.Combine(_outDir, "First.g.cs")).Should().Contain("GetProject(\"Alpha\")");
        File.ReadAllText(Path.Combine(_outDir, "Second.g.cs")).Should().Contain("GetProject(\"Beta\")");
    }

    private void WriteBuild(string memberLine) =>
        File.WriteAllText(_root / "Build.cs",
            $$"""
              partial class Build
              {
                  {{memberLine}}
              }
              """);

    private void WriteSolution(string name, params string[] projectPaths) =>
        File.WriteAllText(_root / name,
            "<Solution>" + Environment.NewLine +
            string.Concat(projectPaths.Select(p => $"  <Project Path=\"{p}\" />{Environment.NewLine}")) +
            "</Solution>");
}
