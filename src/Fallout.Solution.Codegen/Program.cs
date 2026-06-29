using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Solutions;

// Pre-build codegen for the strongly-typed [Solution] accessor.
//
// Usage:
//   Fallout.Solution.Codegen --root <rootDir> --out <outDir> [--source <file> ...]
//
// Discovers [Solution(GenerateProjects = true)] members by parsing the given source files
// syntactically (no compilation available pre-build), resolves each solution file, and writes
// <MemberName>.g.cs into <outDir> using the shared SolutionEmitter. No-op (exit 0) if none found.

var root = GetOption("--root") ?? Directory.GetCurrentDirectory();
var outDir = GetOption("--out") ?? Path.Combine(root, "obj");
var sources = GetOptions("--source").Where(File.Exists).ToList();
if (sources.Count == 0)
    sources = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).ToList();

var rootDirectory = (AbsolutePath)root;
Directory.CreateDirectory(outDir);

foreach (var (memberName, relativePath, fancyNames) in DiscoverSolutionMembers(sources))
{
    var solutionFile = !string.IsNullOrEmpty(relativePath)
        ? rootDirectory / relativePath
        : GetSolutionFileFromParametersFile(rootDirectory, memberName);

    var solution = solutionFile.ReadSolution();
    File.WriteAllText(Path.Combine(outDir, memberName + ".g.cs"), SolutionEmitter.Emit(solution, memberName, fancyNames));
    Console.WriteLine($"Fallout.Solution.Codegen: generated {memberName}.g.cs from {solutionFile}");
}

return 0;

string GetOption(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

IEnumerable<string> GetOptions(string name)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] == name)
            yield return args[i + 1];
}

static IEnumerable<(string MemberName, string RelativePath, bool FancyNames)> DiscoverSolutionMembers(IEnumerable<string> files)
{
    foreach (var file in files)
    {
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
        foreach (var member in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
        {
            if (member is not (FieldDeclarationSyntax or PropertyDeclarationSyntax))
                continue;

            foreach (var attribute in member.AttributeLists.SelectMany(x => x.Attributes))
            {
                if (attribute.Name.ToString() is not ("Solution" or "SolutionAttribute"))
                    continue;

                var arguments = attribute.ArgumentList?.Arguments ?? default;
                var generateProjects = arguments.Any(x =>
                    x.NameEquals?.Name.Identifier.Text == "GenerateProjects" &&
                    x.Expression is LiteralExpressionSyntax { Token.Value: true });
                if (!generateProjects)
                    continue;

                var fancyNames = arguments.Any(x =>
                    x.NameEquals?.Name.Identifier.Text == "FancyNames" &&
                    x.Expression is LiteralExpressionSyntax { Token.Value: true });

                var relativePath = arguments
                    .Where(x => x.NameEquals == null && x.NameColon == null)
                    .Select(x => (x.Expression as LiteralExpressionSyntax)?.Token.Value as string)
                    .FirstOrDefault(x => !string.IsNullOrEmpty(x));

                var memberName = member switch
                {
                    FieldDeclarationSyntax field => field.Declaration.Variables.First().Identifier.Text,
                    PropertyDeclarationSyntax property => property.Identifier.Text,
                    _ => null,
                };

                if (memberName != null)
                    yield return (memberName, relativePath, fancyNames);
            }
        }
    }
}

static AbsolutePath GetSolutionFileFromParametersFile(AbsolutePath rootDirectory, string memberName)
{
    var parametersFile = Constants.GetDefaultParametersFile(rootDirectory);
    Assert.FileExists(parametersFile);
    var obj = JsonNode.Parse(File.ReadAllText(parametersFile)).NotNull().AsObject();
    var solutionRelativePath = obj[memberName].NotNull($"Property '{memberName}' does not exist in '{parametersFile}'.").GetValue<string>();
    return rootDirectory / solutionRelativePath.NotNull();
}
