using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
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

    [Theory]
    [InlineData("Build")]
    [InlineData("build")]
    [InlineData("BUILD")]
    public void Suppresses_itself_when_codegen_mode_is_build(string mode)
    {
        // When the net10 pre-build console owns codegen (FalloutSolutionCodegenMode=Build), the
        // in-compiler generator must no-op so exactly one path emits Solution.g.cs.
        var result = RunWithCodegenMode(mode);

        if (!result.Diagnostics.IsEmpty)
            throw new Exception(string.Join(Environment.NewLine, result.Diagnostics.Select(x => x.GetMessage())));
        result.GeneratedTrees.Should().BeEmpty();
    }

    [Fact]
    public void Still_generates_when_codegen_mode_is_not_build()
    {
        var result = RunWithCodegenMode("Generator");

        result.GeneratedTrees.Should().ContainSingle();
    }

    private static GeneratorDriverRunResult RunWithCodegenMode(string mode)
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

        var options = new TestOptionsProvider(("build_property.FalloutSolutionCodegenMode", mode));
        var driver = CSharpGeneratorDriver.Create(
            new ISourceGenerator[] { new StronglyTypedSolutionGenerator() },
            additionalTexts: null, parseOptions: null, optionsProvider: options);
        return driver.RunGenerators(inputCompilation).GetRunResult();
    }

    private sealed class TestOptionsProvider(params (string Key, string Value)[] global)
        : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new TestOptions(global);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new TestOptions();
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new TestOptions();
    }

    private sealed class TestOptions(params (string Key, string Value)[] entries) : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _values =
            entries.ToDictionary(x => x.Key, x => x.Value);

        public override bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value);
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
