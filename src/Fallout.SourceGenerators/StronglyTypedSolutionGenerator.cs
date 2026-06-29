using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Solutions;
using Fallout.Common.Utilities;

namespace Fallout.SourceGenerators;

/// <summary>
/// In-compiler generator for the strongly-typed <c>[Solution]</c> accessor. This is the
/// <b>toggle fallback</b> (live in-IDE updates); the default is the net10 pre-build
/// <c>Fallout.Solution.Codegen</c> console. Both share <see cref="Fallout.Solutions.SolutionEmitter"/>,
/// so output is identical. The MSBuild targets suppress this generator when console-mode is active.
/// </summary>
[Generator]
public class StronglyTypedSolutionGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            var allTypes = context.Compilation.Assembly.GlobalNamespace.GetAllTypes();
            var members = allTypes.SelectMany(x => x.GetMembers())
                .Where(x => x is IPropertySymbol or IFieldSymbol)
                .Select(x => (Member: x, AttributeData: x.GetAttributeData("global::Fallout.Solutions.SolutionAttribute")))
                .Where(x =>
                    x.AttributeData?.NamedArguments.SingleOrDefault(x => x.Key == "GenerateProjects").Value.Value as bool? ??
                    false)
                .ToList();

            if (members.Count == 0)
                return;

            var rootDirectory = GetRootDirectoryFrom(context.Compilation);

            foreach (var (member, attribute) in members)
            {
                var solutionFile = attribute.ConstructorArguments.FirstOrDefault().Value is string { Length: > 0 } relativeSlnPath
                    ? rootDirectory / relativeSlnPath
                    : GetSolutionFileFromParametersFile(rootDirectory, member.Name);

                var fancyNaming = attribute.NamedArguments.SingleOrDefault(x => x.Key == "FancyNames").Value.Value as bool? ??
                                  false;

                var solution = solutionFile.ReadSolution();
                context.AddSource(member.Name + ".g.cs", SolutionEmitter.Emit(solution, member.Name, fancyNaming));
            }
        }
        catch (Exception exception)
        {
            var diagnostic = Diagnostic.Create(
                "FALLOUT001",
                nameof(StronglyTypedSolutionGenerator),
                exception.Message,
                DiagnosticSeverity.Error,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                warningLevel: 0);

            context.ReportDiagnostic(diagnostic);
        }

        return;

        static AbsolutePath GetSolutionFileFromParametersFile(AbsolutePath rootDirectory, string memberName)
        {
            var parametersFile = Constants.GetDefaultParametersFile(rootDirectory);
            Assert.FileExists(parametersFile);
            var obj = JsonNode.Parse(File.ReadAllText(parametersFile)).NotNull().AsObject();
            var solutionRelativePath = obj[memberName].NotNull($"Property '{memberName}' does not exist in '{parametersFile}'.")
                .GetValue<string>();

            return rootDirectory / solutionRelativePath.NotNull();
        }

        static AbsolutePath GetRootDirectoryFrom(Compilation compilation)
        {
            var syntaxPath = compilation.SyntaxTrees.First().FilePath;
            var startDirectory = Path.GetDirectoryName(File.Exists(syntaxPath)
                ? syntaxPath
                // For testing only
                : Directory.GetCurrentDirectory());

            return Constants.TryGetRootDirectoryFrom(startDirectory).NotNull();
        }
    }
}
