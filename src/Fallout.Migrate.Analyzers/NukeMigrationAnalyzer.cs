using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fallout.Migrate.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NukeMigrationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.NukeNamespaceMigration,
        title: "Migrate Nuke.* references to Fallout.*",
        messageFormat: "'{0}' is part of the legacy Nuke.* surface — migrate to Fallout.* (apply the codefix)",
        category: "Migration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The Nuke.* namespaces and bare types NukeBuild/INukeBuild are provided by the transition shim only. Long-term consumers should reference Fallout.* directly.",
        helpLinkUri: "https://github.com/Fallout-build/Fallout/blob/main/docs/rebrand-plan.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static startContext =>
        {
            // Guard: only fire if the project already references something Fallout.*.
            // Avoids spamming pure-NUKE consumers who haven't started migrating.
            var referencesFallout = false;
            foreach (var assemblyName in startContext.Compilation.ReferencedAssemblyNames)
            {
                if (assemblyName.Name.StartsWith("Fallout.", System.StringComparison.Ordinal))
                {
                    referencesFallout = true;
                    break;
                }
            }

            if (!referencesFallout)
                return;

            startContext.RegisterSyntaxNodeAction(AnalyzeUsing, SyntaxKind.UsingDirective);
            startContext.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
        });
    }

    private static void AnalyzeUsing(SyntaxNodeAnalysisContext context)
    {
        var usingDirective = (UsingDirectiveSyntax)context.Node;
        if (usingDirective.Name is null)
            return;

        var nameText = usingDirective.Name.ToString();
        if (!nameText.StartsWith("Nuke.", System.StringComparison.Ordinal) && nameText != "Nuke")
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, usingDirective.Name.GetLocation(), nameText));
    }

    private static void AnalyzeIdentifier(SyntaxNodeAnalysisContext context)
    {
        var identifier = (IdentifierNameSyntax)context.Node;
        var name = identifier.Identifier.ValueText;

        // Skip identifiers that aren't the leftmost in a qualified chain — we want
        // one diagnostic per offending chain, not one per identifier in it.
        if (identifier.Parent is QualifiedNameSyntax q && q.Right == identifier)
            return;
        if (identifier.Parent is MemberAccessExpressionSyntax m && m.Name == identifier)
            return;

        // Skip the Name of a using directive — AnalyzeUsing already handled it.
        if (FindAncestorUsingDirective(identifier) is { } parentUsing && parentUsing.Name?.Span.Contains(identifier.Span) == true)
            return;

        if (name != "Nuke" && name != "NukeBuild" && name != "INukeBuild")
            return;

        var outermost = WalkUpQualified(identifier);
        context.ReportDiagnostic(Diagnostic.Create(Rule, outermost.GetLocation(), outermost.ToString()));
    }

    private static SyntaxNode WalkUpQualified(SyntaxNode node)
    {
        while (true)
        {
            if (node.Parent is QualifiedNameSyntax q && q.Left == node)
                node = q;
            else if (node.Parent is MemberAccessExpressionSyntax m && m.Expression == node)
                node = m;
            else
                return node;
        }
    }

    private static UsingDirectiveSyntax? FindAncestorUsingDirective(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is UsingDirectiveSyntax u)
                return u;
            // Stop at any statement / member boundary — usings are top-level only.
            if (current is MemberDeclarationSyntax or StatementSyntax)
                return null;
        }
        return null;
    }
}
