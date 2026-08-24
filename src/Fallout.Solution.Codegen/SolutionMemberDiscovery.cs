using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fallout.SolutionCodegen;

/// <summary>
/// A field/property annotated with <c>[Solution(GenerateProjects = true)]</c>, discovered
/// syntactically (no compilation is available pre-build).
/// </summary>
internal record SolutionMember(string MemberName, string RelativePath, bool FancyNames);

/// <summary>
/// Pre-build, purely syntactic discovery of <c>[Solution(GenerateProjects = true)]</c> members.
/// Mirrors the symbol-based <c>StronglyTypedSolutionGenerator</c> as closely as syntax allows so
/// the console and the in-compiler fallback agree on what to emit.
/// </summary>
internal static class SolutionMemberDiscovery
{
    public static IEnumerable<SolutionMember> Discover(IEnumerable<string> files)
    {
        foreach (var file in files)
            foreach (var member in DiscoverInText(File.ReadAllText(file)))
                yield return member;
    }

    public static IEnumerable<SolutionMember> DiscoverInText(string text)
    {
        var root = CSharpSyntaxTree.ParseText(text).GetRoot();
        foreach (var member in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
        {
            if (member is not (FieldDeclarationSyntax or PropertyDeclarationSyntax))
                continue;

            var memberName = GetMemberName(member);
            if (memberName == null)
                continue;

            foreach (var attribute in member.AttributeLists.SelectMany(x => x.Attributes))
            {
                // Match by the rightmost name segment so qualified / aliased usages are recognised:
                // [Solution], [SolutionAttribute], [Solutions.Solution], [Fallout.Solutions.Solution],
                // [global::Fallout.Solutions.SolutionAttribute], ...
                if (GetUnqualifiedAttributeName(attribute) != "Solution")
                    continue;

                var arguments = attribute.ArgumentList?.Arguments ?? default;

                // GenerateProjects / FancyNames are settable properties -> only ever named with '='
                // (NameEquals). 'name:' (NameColon) names a constructor parameter, of which there is
                // only one (relativePath), so it is never valid for these flags.
                if (!arguments.Any(x => IsNamedFlagTrue(x, "GenerateProjects")))
                    continue;

                var fancyNames = arguments.Any(x => IsNamedFlagTrue(x, "FancyNames"));
                var relativePath = GetRelativePath(arguments);

                yield return new SolutionMember(memberName, relativePath, fancyNames);
            }
        }
    }

    /// <summary>
    /// Enumerates <c>*.cs</c> under <paramref name="root"/>, pruning <c>bin</c>/<c>obj</c> and hidden
    /// directories (e.g. <c>.git</c>) so the fallback scan stays fast and never picks up intermediate
    /// or generated sources. Only used when the build doesn't pass explicit <c>--source</c> files.
    /// </summary>
    public static IEnumerable<string> EnumerateProjectSources(string root)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*.cs"))
            yield return file;

        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(directory);
            if (name is "bin" or "obj" || name.StartsWith("."))
                continue;

            foreach (var file in EnumerateProjectSources(directory))
                yield return file;
        }
    }

    private static string GetUnqualifiedAttributeName(AttributeSyntax attribute)
    {
        var name = attribute.Name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name.Identifier.Text,
            SimpleNameSyntax simple => simple.Identifier.Text,
            var other => other.ToString(),
        };

        const string suffix = "Attribute";
        return name.EndsWith(suffix) ? name[..^suffix.Length] : name;
    }

    private static bool IsNamedFlagTrue(AttributeArgumentSyntax argument, string name) =>
        argument.NameEquals?.Name.Identifier.Text == name &&
        argument.Expression is LiteralExpressionSyntax { Token.Value: true };

    private static string GetRelativePath(SeparatedSyntaxList<AttributeArgumentSyntax> arguments) =>
        arguments
            // The relativePath constructor argument: positional, or written 'relativePath:' (NameColon).
            // Exclude property assignments (NameEquals) like GenerateProjects = true.
            .Where(x => x.NameEquals == null &&
                        (x.NameColon == null || x.NameColon.Name.Identifier.Text == "relativePath"))
            .Select(x => (x.Expression as LiteralExpressionSyntax)?.Token.Value as string)
            .FirstOrDefault(x => !string.IsNullOrEmpty(x));

    private static string GetMemberName(MemberDeclarationSyntax member) =>
        member switch
        {
            FieldDeclarationSyntax field => field.Declaration.Variables.First().Identifier.Text,
            PropertyDeclarationSyntax property => property.Identifier.Text,
            _ => null,
        };
}
