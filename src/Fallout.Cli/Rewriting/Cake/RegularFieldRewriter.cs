using Fallout.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fallout.Cli.Rewriting.Cake;

internal class RegularFieldRewriter : SafeSyntaxRewriter
{
    public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        node = (FieldDeclarationSyntax)base.VisitFieldDeclaration(node).NotNull();
        var initializerValue = node.GetSingleDeclarator().Initializer?.Value;
        if (initializerValue != null)
        {
            node = node.WithDeclaration(node.Declaration
                .WithType(initializerValue.GetExpressionType()));
        }

        return node;
    }
}
