using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fallout.Migrate.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NukeMigrationCodeFix))]
public sealed class NukeMigrationCodeFix : CodeFixProvider
{
    private const string Title = "Migrate to Fallout.*";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.NukeNamespaceMigration);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            if (node is null)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: ct => ApplyFixAsync(context.Document, node, ct),
                    equivalenceKey: Title),
                diagnostic);
        }
    }

    private static async Task<Document> ApplyFixAsync(Document document, SyntaxNode offendingNode,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var rewriter = new NukeToFalloutRewriter();
        var rewritten = rewriter.Visit(offendingNode);
        var newRoot = root.ReplaceNode(offendingNode, rewritten);
        return document.WithSyntaxRoot(newRoot);
    }

    private sealed class NukeToFalloutRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            var replacement = node.Identifier.ValueText switch
            {
                "Nuke" => "Fallout",
                "NukeBuild" => "FalloutBuild",
                "INukeBuild" => "IFalloutBuild",
                _ => null,
            };

            if (replacement is null)
            {
                return base.VisitIdentifierName(node);
            }

            return node.WithIdentifier(SyntaxFactory.Identifier(
                node.Identifier.LeadingTrivia,
                replacement,
                node.Identifier.TrailingTrivia));
        }
    }
}
