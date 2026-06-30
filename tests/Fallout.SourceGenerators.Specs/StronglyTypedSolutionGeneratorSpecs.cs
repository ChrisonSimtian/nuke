using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Fallout.Common;
using Fallout.Solutions;
using VerifyXunit;
using Xunit;

namespace Fallout.SourceGenerators.Specs;

public class StronglyTypedSolutionGeneratorSpecs
{
    [Fact]
    public Task Enabled_code_generation()
    {
        var inputCompilation = CreateCompilation("""
                                                 using Fallout.Common;
                                                 using Fallout.Solutions;
                                                 partial class Build : FalloutBuild
                                                 {
                                                     [Solution(GenerateProjects = true)]
                                                     readonly Solution Solution;
                                                 }
                                                 """);

        var generator = new StronglyTypedSolutionGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(inputCompilation);
        return Verifier.Verify(result);
    }

    [Fact]
    public Task Enabled_code_generation_with_fancy_naming()
    {
        var inputCompilation = CreateCompilation("""
                                                 using Fallout.Common;
                                                 using Fallout.Solutions;
                                                 partial class Build : FalloutBuild
                                                 {
                                                     [Solution(GenerateProjects = true, FancyNames = true)]
                                                     readonly Solution Solution;
                                                 }
                                                 """);

        var generator = new StronglyTypedSolutionGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(inputCompilation);
        return Verifier.Verify(result);
    }

    [Fact]
    public void Disable_code_generation()
    {
        var inputCompilation = CreateCompilation("""

                                                 using Fallout.Common;
                                                 using Fallout.Solutions;

                                                 partial class Build : FalloutBuild
                                                 {
                                                     [Solution(GenerateProjects = false)]
                                                     readonly Solution Solution;
                                                 }
                                                 """);

        var generator = new StronglyTypedSolutionGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(inputCompilation).GetRunResult();

        if (!result.Diagnostics.IsEmpty)
            throw new Exception(string.Join(Environment.NewLine, result.Diagnostics.Select(x => x.GetMessage())));

        result.GeneratedTrees.Should().BeEmpty();
    }

    [Fact]
    public void Disable_code_generation_with_enabled_fancy_naming()
    {
        var inputCompilation = CreateCompilation("""

                                                 using Fallout.Common;
                                                 using Fallout.Solutions;

                                                 partial class Build : FalloutBuild
                                                 {
                                                     [Solution(GenerateProjects = false, FancyNames = true)]
                                                     readonly Solution Solution;
                                                 }
                                                 """);

        var generator = new StronglyTypedSolutionGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(inputCompilation).GetRunResult();

        if (!result.Diagnostics.IsEmpty)
            throw new Exception(string.Join(Environment.NewLine, result.Diagnostics.Select(x => x.GetMessage())));

        result.GeneratedTrees.Should().BeEmpty();
    }

    [Fact]
    public void Unspecified_code_generation()
    {
        var inputCompilation = CreateCompilation("""

                                                 using Fallout.Common;
                                                 using Fallout.Solutions;

                                                 partial class Build : FalloutBuild
                                                 {
                                                     [Solution]
                                                     readonly Solution Solution;
                                                 }
                                                 """);

        var generator = new StronglyTypedSolutionGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(inputCompilation).GetRunResult();

        if (!result.Diagnostics.IsEmpty)
            throw new Exception(string.Join(Environment.NewLine, result.Diagnostics.Select(x => x.GetMessage())));

        result.GeneratedTrees.Should().BeEmpty();
    }

    [Fact]
    public void Unspecified_code_generation_with_enabled_fancy_naming()
    {
        var inputCompilation = CreateCompilation("""

                                                 using Fallout.Common;
                                                 using Fallout.Solutions;

                                                 partial class Build : FalloutBuild
                                                 {
                                                     [Solution(FancyNames = true)]
                                                     readonly Solution Solution;
                                                 }
                                                 """);

        var generator = new StronglyTypedSolutionGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var result = driver.RunGenerators(inputCompilation).GetRunResult();

        if (!result.Diagnostics.IsEmpty)
            throw new Exception(string.Join(Environment.NewLine, result.Diagnostics.Select(x => x.GetMessage())));

        result.GeneratedTrees.Should().BeEmpty();
    }

    private static Compilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create("compilation",
            new[]
            {
                CSharpSyntaxTree.ParseText(source)
            },
            Basic.Reference.Assemblies.NetStandard20.References.All
                .Concat(new[]
                    {
                        typeof(FalloutBuild),
                        typeof(SolutionAttribute)
                    }
                    .Select(x => MetadataReference.CreateFromFile(x.Assembly.Location))),
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}
