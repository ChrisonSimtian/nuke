using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.Common.IO;
using Fallout.SolutionCodegen;
using Fallout.Solutions;
using Fallout.SourceGenerators;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Fallout.Solution.Codegen.Specs;

// Parity contract for the two Solution.g.cs producers:
//   - the in-compiler StronglyTypedSolutionGenerator (the toggle fallback; symbol-based discovery), and
//   - the net10 pre-build console (the default when FalloutSolutionCodegenMode=Build; syntactic discovery).
//
// They share SolutionEmitter, so emission is identical by construction; what this pins is that the two
// DISCOVERY paths agree on WHAT to emit, and that neither drifts from the expected output. Every case
// asserts both (a) the two paths produce the same text and (b) that text is the expected accessor.
public class GeneratorConsoleParitySpecs : IDisposable
{
    private readonly AbsolutePath _root;

    public GeneratorConsoleParitySpecs()
    {
        _root = (AbsolutePath)Path.Combine(Path.GetTempPath(), "fallout-parity-" + Path.GetRandomFileName());
        // The generator locates the repo root by walking up for a .fallout marker directory.
        Directory.CreateDirectory(_root / ".fallout");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Both_paths_produce_identical_and_correct_output()
    {
        WriteSolution("app.slnx", "src/Alpha/Alpha.csproj", "src/Beta/Beta.csproj");
        var buildCs = WriteBuild("""[Solution("app.slnx", GenerateProjects = true)] readonly Solution Solution;""");

        var console = RunConsole(buildCs);
        var generator = RunGenerator(buildCs);

        // (a) the two paths agree
        generator.Should().Be(console);

        // (b) the output is the expected accessor: a Solution class with one Project member per project,
        //     ordered by name, using the plain '_' delimiter (FancyNames off).
        console.Should()
            .Contain("internal class Solution(SolutionModel model, AbsolutePath path) : Fallout.Solutions.Solution(model, path)")
            .And.Contain("public Fallout.Solutions.Project Alpha => this.GetProject(\"Alpha\");")
            .And.Contain("public Fallout.Solutions.Project Beta => this.GetProject(\"Beta\");");
    }

    [Fact]
    public void Both_paths_agree_on_fancy_naming()
    {
        // FancyNames is the one flag whose delimiter actually differs between modes (U+A78F vs '_'),
        // so this is the case most likely to expose a divergence between the two producers.
        WriteSolution("app.slnx", "src/My.App/My.App.csproj");
        var buildCs = WriteBuild("""[Solution("app.slnx", GenerateProjects = true, FancyNames = true)] readonly Solution Solution;""");

        var console = RunConsole(buildCs);

        RunGenerator(buildCs).Should().Be(console);
        console.Should().Contain("public Fallout.Solutions.Project MyꞏApp => this.GetProject(\"My.App\");"); // U+A78F, not '_'
    }

    [Theory]
    [InlineData("[Solution(\"app.slnx\", GenerateProjects = true)]")]
    [InlineData("[SolutionAttribute(\"app.slnx\", GenerateProjects = true)]")]
    [InlineData("[Fallout.Solutions.Solution(\"app.slnx\", GenerateProjects = true)]")]
    [InlineData("[Solution(relativePath: \"app.slnx\", GenerateProjects = true)]")]
    public void Both_paths_agree_across_attribute_spellings(string attribute)
    {
        WriteSolution("app.slnx", "src/Alpha/Alpha.csproj");
        var buildCs = WriteBuild($"{attribute} readonly Solution Solution;");

        RunGenerator(buildCs).Should().Be(RunConsole(buildCs));
    }

    [Fact]
    public void Both_paths_agree_that_generate_projects_false_emits_nothing()
    {
        WriteSolution("app.slnx", "src/Alpha/Alpha.csproj");
        var buildCs = WriteBuild("""[Solution("app.slnx", GenerateProjects = false)] readonly Solution Solution;""");

        RunConsole(buildCs).Should().BeNull();      // no Solution.g.cs written
        RunGenerator(buildCs).Should().BeNull();    // no generated tree
    }

    // Drives the net10 console pipeline; returns the emitted Solution.g.cs, or null if none was produced.
    private string RunConsole(string buildCsPath)
    {
        var outDir = _root / "obj-console";
        SolutionCodegenRunner.Run(_root, outDir, new[] { buildCsPath });

        var file = outDir / "Solution.g.cs";
        return File.Exists(file) ? Normalize(File.ReadAllText(file)) : null;
    }

    // Drives the in-compiler generator over the same source; returns the generated Solution.g.cs, or null.
    private static string RunGenerator(string buildCsPath)
    {
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(buildCsPath), path: buildCsPath);
        var compilation = CSharpCompilation.Create("parity",
            new[] { tree },
            Basic.Reference.Assemblies.NetStandard20.References.All.Concat(SolutionAttributeReferences),
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        var result = CSharpGeneratorDriver.Create(new StronglyTypedSolutionGenerator())
            .RunGenerators(compilation)
            .GetRunResult();

        result.Diagnostics.Should().BeEmpty("the generator should not report FALLOUT001 for a valid [Solution]");
        var generated = result.GeneratedTrees.SingleOrDefault();
        return generated == null ? null : Normalize(generated.GetText().ToString());
    }

    // SolutionAttribute derives from ParameterAttribute (in Fallout.Build); the attribute only binds -
    // and the generator only discovers the member - when its whole base-type chain is referenced. Pulled
    // in by assembly location via reflection to avoid naming FalloutBuild, which is ambiguous here (it is
    // exported by both Fallout.Build and the source-linked Fallout.SourceGenerators).
    private static readonly MetadataReference[] SolutionAttributeReferences =
        EnumerateBaseTypes(typeof(SolutionAttribute))
            .Select(x => x.Assembly)
            // System.* comes from the NetStandard20 facade set; adding the runtime CoreLib on top breaks
            // core-type resolution. Keep only the Fallout.* assemblies the attribute chain lives in.
            .Where(x => x.GetName().Name?.StartsWith("Fallout", StringComparison.Ordinal) == true)
            .Select(x => x.Location)
            .Distinct()
            .Select(x => (MetadataReference)MetadataReference.CreateFromFile(x))
            .ToArray();

    private static IEnumerable<Type> EnumerateBaseTypes(Type type)
    {
        for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            yield return current;
    }

    private static string Normalize(string text) => text.ReplaceLineEndings("\n");

    private string WriteBuild(string memberLine)
    {
        var path = _root / "Build.cs";
        File.WriteAllText(path,
            $$"""
              using Fallout.Solutions;
              partial class Build
              {
                  {{memberLine}}
              }
              """);
        return path;
    }

    private void WriteSolution(string name, params string[] projectPaths) =>
        File.WriteAllText(_root / name,
            "<Solution>" + Environment.NewLine +
            string.Concat(projectPaths.Select(p => $"  <Project Path=\"{p}\" />{Environment.NewLine}")) +
            "</Solution>");
}
