using System.Collections.Generic;
using Fallout.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Fallout.Cli.Rewriting.Cake;

internal class MemberAccessRewriter : SafeSyntaxRewriter
{
    private Dictionary<string, string> Replacements =>
        new()
        {
            ["BuildSystem"] = null,
        };

    public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        node = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node).NotNull();
        return node.Expression is not IdentifierNameSyntax identifierNameSyntax ||
               !Replacements.TryGetValue(identifierNameSyntax.Identifier.Text, out var newName)
            ? node
            : newName != null
                ? node.WithExpression(identifierNameSyntax.WithIdentifier(Identifier(newName)))
                : node.Name;
    }
}
