using System;
using System.IO;
using FluentAssertions;
using Fallout.Common.IO;
using Fallout.Solutions;
using Xunit;

namespace Fallout.Solution.Codegen.Specs;

public class SolutionEmitterSpecs : IDisposable
{
    private readonly AbsolutePath _root;

    public SolutionEmitterSpecs()
    {
        _root = (AbsolutePath)Path.Combine(Path.GetTempPath(), "fallout-emitter-" + Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Escapes_dotted_project_names_into_valid_identifiers()
    {
        var solution = WriteAndReadSolution("src/Foo.Bar/Foo.Bar.csproj");

        var emitted = SolutionEmitter.Emit(solution, "Sln", fancyNaming: false);

        emitted.Should().Contain("Foo_Bar => this.GetProject(\"Foo.Bar\")");
    }

    [Fact]
    public void Prefixes_underscore_when_a_project_name_starts_with_a_digit()
    {
        var solution = WriteAndReadSolution("src/00-Build/00-Build.csproj");

        var emitted = SolutionEmitter.Emit(solution, "Sln", fancyNaming: false);

        emitted.Should().Contain("_00_Build => this.GetProject(\"00-Build\")");
    }

    [Fact]
    public void Emits_nested_declarations_for_solution_folders()
    {
        var slnx = _root / "app.slnx";
        File.WriteAllText(slnx,
            "<Solution>" + Environment.NewLine +
            "  <Folder Name=\"/group/\">" + Environment.NewLine +
            "    <Project Path=\"src/Inner/Inner.csproj\" />" + Environment.NewLine +
            "  </Folder>" + Environment.NewLine +
            "</Solution>");

        var emitted = SolutionEmitter.Emit(slnx.ReadSolution(), "Sln", fancyNaming: false);

        emitted.Should().Contain("Unsafe.As<").And.Contain("this.GetSolutionFolder(");
        emitted.Should().Contain(": Fallout.Solutions.SolutionFolder(");
        emitted.Should().Contain("GetProject(\"Inner\")");
    }

    private Fallout.Solutions.Solution WriteAndReadSolution(string projectPath)
    {
        var slnx = _root / "app.slnx";
        File.WriteAllText(slnx, $"<Solution>{Environment.NewLine}  <Project Path=\"{projectPath}\" />{Environment.NewLine}</Solution>");
        return slnx.ReadSolution();
    }
}
