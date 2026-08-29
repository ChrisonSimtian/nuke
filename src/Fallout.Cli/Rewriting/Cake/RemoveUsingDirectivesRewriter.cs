using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fallout.Cli.Rewriting.Cake;

internal class RemoveUsingDirectivesRewriter : SafeSyntaxRewriter
{
    public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
    {
        return null;
    }
}
